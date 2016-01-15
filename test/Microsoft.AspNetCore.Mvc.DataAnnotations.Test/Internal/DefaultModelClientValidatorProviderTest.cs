// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations.Internal
{
    // Integration tests for the default configuration of ModelMetadata and Validation providers
    public class DefaultModelClientValidatorProviderTest
    {
        [Fact]
        public void GetValidators_ForIValidatableObject()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForType(typeof(ValidatableObject));
            var context = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validatorItems = context.Results;

            var validatorItem = Assert.Single(validatorItems);
            Assert.IsType<ValidatableObjectAdapter>(validatorItem.Validator);
        }

        [Fact]
        public void GetValidators_ClientModelValidatorAttributeOnClass()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForType(typeof(ModelValidatorAttributeOnClass));
            var context = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validatorItems = context.Results;

            var validatorItem = Assert.Single(validatorItems);
            var customModelValidator = Assert.IsType<CustomModelValidatorAttribute>(validatorItem.Validator);
            Assert.Equal("Class", customModelValidator.Tag);
        }

        [Fact]
        public void GetValidators_ClientModelValidatorAttributeOnProperty()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelValidatorAttributeOnProperty),
                nameof(ModelValidatorAttributeOnProperty.Property));
            var context = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validatorItems = context.Results;

            var validatorItem = Assert.IsType<CustomModelValidatorAttribute>(Assert.Single(validatorItems).Validator);
            Assert.Equal("Property", validatorItem.Tag);
        }

        [Fact]
        public void GetValidators_ClientModelValidatorAttributeOnPropertyAndClass()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ModelValidatorAttributeOnPropertyAndClass),
                nameof(ModelValidatorAttributeOnPropertyAndClass.Property));
            var context = new ModelValidatorProviderContext(metadata, GetValidatorItems(metadata));

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validatorItems = context.Results;

            Assert.Equal(2, validatorItems.Count);
            Assert.Single(validatorItems, v => Assert.IsType<CustomModelValidatorAttribute>(v.Validator).Tag == "Class");
            Assert.Single(validatorItems, v => Assert.IsType<CustomModelValidatorAttribute>(v.Validator).Tag == "Property");
        }

        // RangeAttribute is an example of a ValidationAttribute with it's own adapter type.
        [Fact]
        public void GetValidators_ClientValidatorAttribute_SpecificAdapter()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestClientModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(RangeAttributeOnProperty),
                nameof(RangeAttributeOnProperty.Property));
            var context = new ClientValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

            Assert.Equal(2, validators.Count);
            Assert.Single(validators, v => v is RangeAttributeAdapter);
            Assert.Single(validators, v => v is RequiredAttributeAdapter);
        }

        [Fact]
        public void GetValidators_ClientValidatorAttribute_DefaultAdapter()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestClientModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(CustomValidationAttributeOnProperty),
                nameof(CustomValidationAttributeOnProperty.Property));
            var context = new ClientValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

             Assert.IsType<CustomValidationAttribute>(Assert.Single(validators));
        }

        [Fact]
        public void GetValidators_FromModelMetadataType_SingleValidator()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestClientModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ProductViewModel),
                nameof(ProductViewModel.Id));
            var context = new ClientValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

            Assert.Equal(2, validators.Count);
            Assert.Single(validators, v => v is RangeAttributeAdapter);
            Assert.Single(validators, v => v is RequiredAttributeAdapter);
        }

        [Fact]
        public void GetValidators_FromModelMetadataType_MergedValidators()
        {
            // Arrange
            var metadataProvider = TestModelMetadataProvider.CreateDefaultProvider();
            var validatorProvider = TestClientModelValidatorProvider.CreateDefaultProvider();

            var metadata = metadataProvider.GetMetadataForProperty(
                typeof(ProductViewModel),
                nameof(ProductViewModel.Name));
            var context = new ClientValidatorProviderContext(metadata);

            // Act
            validatorProvider.GetValidators(context);

            // Assert
            var validators = context.Validators;

            Assert.Equal(2, validators.Count);
            Assert.Single(validators, v => v is RegularExpressionAttributeAdapter);
            Assert.Single(validators, v => v is StringLengthAttributeAdapter);
        }

        private IList<ValidatorItem> GetValidatorItems(ModelMetadata metadata)
        {
            var items = new List<ValidatorItem>(metadata.ValidatorMetadata.Count);
            for (var i = 0; i < metadata.ValidatorMetadata.Count; i++)
            {
                items.Add(new ValidatorItem(metadata.ValidatorMetadata[i]));
            }

            return items;
        }

        private class ValidatableObject : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                return null;
            }
        }

        [CustomModelValidator(Tag = "Class")]
        private class ModelValidatorAttributeOnClass
        {
        }

        private class ModelValidatorAttributeOnProperty
        {
            [CustomModelValidator(Tag = "Property")]
            public string Property { get; set; }
        }

        private class ModelValidatorAttributeOnPropertyAndClass
        {
            [CustomModelValidator(Tag = "Property")]
            public ModelValidatorAttributeOnClass Property { get; set; }
        }

        private class CustomModelValidatorAttribute : Attribute, IModelValidator
        {
            public string Tag { get; set; }

            public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class RangeAttributeOnProperty
        {
            [Range(0, 10)]
            public int Property { get; set; }
        }

        private class CustomValidationAttribute : Attribute, IClientModelValidator
        {
            public void AddValidation(ClientModelValidationContext context)
            {
            }
        }

        private class CustomValidationAttributeOnProperty
        {
            [CustomValidation]
            public string Property { get; set; }
        }

        private class ProductEntity
        {
            [Range(0, 10)]
            public int Id { get; set; }

            [RegularExpression(".*")]
            public string Name { get; set; }
        }

        [ModelMetadataType(typeof(ProductEntity))]
        private class ProductViewModel
        {
            public int Id { get; set; }

            [StringLength(4)]
            public string Name { get; set; }
        }
    }
}