using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PQBI.Network.RestApi.Validations
{

    public abstract class ServiceValidatorBase
    {
        protected virtual IList<PropertyServiceError> Dissect(ValidationResult validationResult)
        {
            var errors = new List<PropertyServiceError>();
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    errors.Add(new PropertyServiceError(errorMessage: error.ErrorMessage, title: error.PropertyName));
                }
            }

            return errors;
        }


        protected void Validate(IEnumerable<PropertyServiceError> errors)
        {
            if (errors is not null && errors.Count() > 0)
            {
                var instance = new PQBIException(errors.First());
                throw instance;
            }
        }

        //protected void Validate(IEnumerable<GlobalServiceError> errors)
        //{
        //    if (errors is not null && errors.Count() > 0)
        //    {
        //        var instance = new ChatoException(errors.ToArray());
        //        throw instance;
        //    }
        //}
    }
}