using Net.Chdk.Model.Software;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Net.Chdk.Validators.Software
{
    sealed class ModulesValidator : SoftwareValidator<ModulesInfo>
    {
        public ModulesValidator(IValidator<SoftwareHashInfo> hashValidator)
            : base(hashValidator)
        {
        }

        protected override void DoValidate(ModulesInfo modules, string basePath)
        {
            Validate(modules.Version);
            Validate(modules.Modules, basePath);
        }

        private void Validate(IDictionary<string, ModuleInfo> modules, string basePath)
        {
            if (modules == null)
                throw new ValidationException("Null modules");

            foreach (var kvp in modules)
                Validate(kvp.Key, kvp.Value, basePath);
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
    }
}
