using Net.Chdk.Model.Software;
using Net.Chdk.Providers.Boot;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Net.Chdk.Validators.Software
{
    abstract class SoftwareValidator<T> : Validator<T>
    {
        protected IValidator<SoftwareHashInfo> HashValidator { get; }

        protected SoftwareValidator(IValidator<SoftwareHashInfo> hashValidator)
        {
            HashValidator = hashValidator;
        }
    }

    sealed class SoftwareValidator : SoftwareValidator<SoftwareInfo>
    {
        private IBootProviderResolver BootProviderResolver { get; }

        public SoftwareValidator(IBootProviderResolver bootProviderResolver, IValidator<SoftwareHashInfo> hashValidator)
            : base(hashValidator)
        {
            BootProviderResolver = bootProviderResolver;
        }

        protected override void DoValidate(SoftwareInfo software, string basePath, IProgress<double> progress)
        {
            Validate(software.Version);
            Validate(software.Product);
            Validate(software.Camera);
            Validate(software.Build);
            Validate(software.Compiler);
            Validate(software.Source);
            Validate(software.Encoding);
            Validate(software.Hash, basePath, software.Product.Category, progress);
        }

        private static void Validate(SoftwareProductInfo product)
        {
            if (product == null)
                throw new ValidationException("Null product");

            if (string.IsNullOrEmpty(product.Name))
                throw new ValidationException("Missing product name");

            if (string.IsNullOrEmpty(product.Category))
                throw new ValidationException("Missing product category");

            if (product.Version == null)
                throw new ValidationException("Null product version");

            if (product.Version.Major < 0 || product.Version.Minor < 0)
                throw new ValidationException("Invalid product version");

            ValidateCreated(product.Created, () => "product");

            if (product.Language == null)
                throw new ValidationException("Invalid product language");
        }

        private static void Validate(SoftwareCameraInfo camera)
        {
            if (camera == null)
                throw new ValidationException("Null camera");

            if (string.IsNullOrEmpty(camera.Platform))
                throw new ValidationException("Null camera platform");

            if (string.IsNullOrEmpty(camera.Revision))
                throw new ValidationException("Null camera revision");
        }

        private static void Validate(SoftwareBuildInfo build)
        {
            if (build == null)
                throw new ValidationException("Null build");

            // Empty in update
            if (build.Name == null)
                throw new ValidationException("Null build name");

            // Empty in final
            if (build.Status == null)
                throw new ValidationException("Null build status");

            ValidateChangeset(build.Changeset, () => "build");
        }

        private static void Validate(SoftwareCompilerInfo compiler)
        {
            // Unknown in download
            if (compiler == null)
                return;

            if (string.IsNullOrEmpty(compiler.Name))
                throw new ValidationException("Missing compiler name");

            if (compiler.Version == null)
                throw new ValidationException("Null compiler version");
        }

        private static void Validate(SoftwareSourceInfo source)
        {
            // Missing in manual build
            if (source == null)
                return;

            if (string.IsNullOrEmpty(source.Name))
                throw new ValidationException("Missing source name");

            if (source.Channel == null)
                throw new ValidationException("Missing source channel");

            if (source.Url == null)
                throw new ValidationException("Missing source url");
        }

        private void Validate(SoftwareEncodingInfo encoding)
        {
            // Missing if undetected
            if (encoding == null)
                return;

            if (encoding.Name == null)
                throw new ValidationException("Missing encoding name");

            if (encoding.Name.Length > 0 && encoding.Data == null)
                throw new ValidationException("Missing encoding data");
        }

        private void Validate(SoftwareHashInfo hash, string basePath, string categoryName, IProgress<double> progress)
        {
            if (hash == null)
                ThrowValidationException("Null hash");

            var bootProvider = BootProviderResolver.GetBootProvider(categoryName);
            if (bootProvider == null)
                ThrowValidationException("Missing {0} boot provider", categoryName);

            HashValidator.Validate(hash, basePath, progress);

            var fileName = bootProvider.FileName;
            if (!hash.Values.Keys.Contains(fileName, StringComparer.InvariantCultureIgnoreCase))
                ThrowValidationException("Missing {0}", fileName);
        }
    }
}
