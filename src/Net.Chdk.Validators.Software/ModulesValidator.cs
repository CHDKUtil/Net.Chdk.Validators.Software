using Net.Chdk.Model.Software;
using Net.Chdk.Providers.Software;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Net.Chdk.Validators.Software
{
    sealed class ModulesValidator : SoftwareValidator<ModulesInfo>
    {
        private IModulesProviderResolver ModulesProviderResolver { get; }

        public ModulesValidator(IModulesProviderResolver modulesProviderResolver, IValidator<SoftwareHashInfo> hashValidator)
            : base(hashValidator)
        {
            ModulesProviderResolver = modulesProviderResolver;
        }

        protected override void DoValidate(ModulesInfo modules, string basePath)
        {
            Validate(modules.Version);
            Validate(modules.ProductName);
            Validate(modules.ProductName, modules.Modules, basePath);
        }

        private static void Validate(string productName)
        {
            if (string.IsNullOrEmpty(productName))
                throw new ValidationException("Missing product name");
        }

        private void Validate(string productName, IDictionary<string, ModuleInfo> modules, string basePath)
        {
            if (modules == null)
                ThrowValidationException("Null modules");

            var values = new Dictionary<string, string>();
            foreach (var kvp in modules)
            {
                Validate(kvp.Key, kvp.Value, basePath);
                foreach (var kvp2 in kvp.Value.Hash.Values)
                    values.Add(kvp2.Key, kvp2.Value);
            }

            var modulesProvider = ModulesProviderResolver.GetModulesProvider(productName);
            if (modulesProvider == null)
                ThrowValidationException("Missing {0} modules provider", productName);

            Validate(modulesProvider, values, basePath);
        }

        private void Validate(string name, ModuleInfo module, string basePath)
        {
            if (string.IsNullOrEmpty(name))
                ThrowValidationException("Missing module name");

            Func<string> formatter = () => string.Format("module {0}", name);

            if (module == null)
                ThrowValidationException("Null {0}", formatter);

            ValidateCreated(module.Created, formatter);
            ValidateChangeset(module.Changeset, formatter);

            HashValidator.Validate(module.Hash, basePath);
        }

        private static void Validate(IModulesProvider modulesProvider, Dictionary<string, string> values, string basePath)
        {
            var modulesPath = modulesProvider.Path;
            var path = Path.Combine(basePath, modulesPath);
            if (!Directory.Exists(path))
                ThrowValidationException("Missing {0}", path);

            var pattern = string.Format("*{0}", modulesProvider.Extension);
            var files = Directory.EnumerateFiles(path, pattern);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var filePath = Path.Combine(modulesPath, fileName).ToLowerInvariant();
                if (!values.ContainsKey(filePath))
                    ThrowValidationException("Missing {0}", fileName);
            }
        }
    }
}
