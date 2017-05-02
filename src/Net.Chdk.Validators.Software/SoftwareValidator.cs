using Net.Chdk.Model.Software;
using Net.Chdk.Providers.Boot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Net.Chdk.Validators.Software
{
    sealed class SoftwareValidator : IValidator<SoftwareInfo>
    {
        private static readonly string[] SecureHashes = new[] { "sha256", "sha384", "sha512" };

        private IBootProvider BootProvider { get; }

        public SoftwareValidator(IBootProvider bootProvider)
        {
            BootProvider = bootProvider;
        }

        public void Validate(SoftwareInfo software, string basePath)
        {
            if (software == null)
                throw new ArgumentNullException(nameof(software));

            Validate(software.Version);
            Validate(software.Product);
            Validate(software.Camera);
            Validate(software.Build);
            Validate(software.Compiler);
            Validate(software.Source);
            Validate(software.Hash, basePath);
        }

        private static void Validate(Version version)
        {
            if (version == null)
                throw new ValidationException("Null version");

            if (version.Major < 1 || version.Minor < 0)
                throw new ValidationException("Invalid version");
        }

        private static void Validate(SoftwareProductInfo product)
        {
            if (product == null)
                throw new ValidationException("Null product");

            if (string.IsNullOrEmpty(product.Name))
                throw new ValidationException("Missing product name");

            if (product.Version == null)
                throw new ValidationException("Null product version");

            if (product.Version.Major < 0 || product.Version.Minor < 0)
                throw new ValidationException("Invalid product version");

            if (product.Version.MajorRevision < 0 || product.Version.MinorRevision < 0)
                throw new ValidationException("Invalid product version");

            if (product.Created == null)
                throw new ValidationException("Null product created");

            if (product.Created.Value < new DateTime(2000, 1, 1) || product.Created.Value > DateTime.UtcNow)
                throw new ValidationException("Invalid product created");

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

            if (string.IsNullOrEmpty(source.Channel))
                throw new ValidationException("Missing source channel");

            if (source.Url == null)
                throw new ValidationException("Missing source url");
        }

        private void Validate(SoftwareHashInfo hash, string basePath)
        {
            if (hash == null)
                throw new ValidationException("Null hash");

            if (string.IsNullOrEmpty(hash.Name))
                throw new ValidationException("Missing hash name");

            if (!SecureHashes.Contains(hash.Name))
                throw new ValidationException("Invalid hash name");

            if (!Validate(hash.Values, hash.Name, basePath))
                throw new ValidationException("Mismatching hash");
        }

        private bool Validate(IDictionary<string, string> hashValues, string hashName, string basePath)
        {
            if (hashValues == null)
                return false;

            if (hashValues.Count == 0)
                return false;

            if (!hashValues.Keys.Contains(BootProvider.FileName, StringComparer.InvariantCultureIgnoreCase))
                return false;

            foreach (var kvp in hashValues)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                    return false;

                if (string.IsNullOrEmpty(kvp.Value))
                    return false;

                var fileName = kvp.Key.ToUpperInvariant();
                var filePath = Path.Combine(basePath, fileName);
                if (!File.Exists(filePath))
                    return false;

                var hashString = GetHashString(filePath, hashName);
                if (!hashString.Equals(kvp.Value))
                    return false;
            }

            return true;
        }

        private static string GetHashString(string filePath, string hashName)
        {
            var hash = ComputeHash(filePath, hashName);
            var sb = new StringBuilder(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }

        private static byte[] ComputeHash(string filePath, string hashName)
        {
            var hashAlgorithm = HashAlgorithm.Create(hashName);
            using (var stream = File.OpenRead(filePath))
            {
                return hashAlgorithm.ComputeHash(stream);
            }
        }
    }
}
