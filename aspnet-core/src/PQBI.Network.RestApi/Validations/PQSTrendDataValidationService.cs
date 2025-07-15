//using Castle.Components.DictionaryAdapter;
//using FluentValidation;
//using Microsoft.Extensions.Logging;
//using PQBI.PQS.CalcEngine;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace PQBI.Network.RestApi.Validations
//{
//    public interface IPQSTrendDataValidationService
//    {
//        Task RegistrationRequestValidateAsync(CalcRequestDto request);
//    }


//    public class PQSTrendDataValidationService : ServiceValidatorBase, IPQSTrendDataValidationService
//    {
//        internal class CalcRequestDtoValidator : AbstractValidator<CalcRequestDto>
//        { 
//            public CalcRequestDtoValidator()
//            {

//                RuleFor(req => req.Resolution)
//                    .NotEmpty()
//                    .WithMessage("{PropertyName} shouldn't be empty.");


//                RuleFor(req => req.CustomParameterIds)
//                   .NotEmpty()
//                   .WithMessage("{PropertyName} should not be empty.")
//                   .Must(customParams => customParams.All(cp => cp.Value != null && cp.Value.ParameterList != null && cp.Value.ParameterList.Count > 0))
//                   .WithMessage("Each CustomParameters.Value.ParameterList should not be empty.");


//                RuleForEach(req => req.CustomParameterIds)
//                    .ChildRules(customParams =>
//                    {
//                        customParams.RuleFor(cp => cp.Value.Type)
//                            .Must(type => type == "SPMC" || type == "MPSC" || type == "EXCEPTION")
//                            .WithMessage("Type must be either 'SPMC' or 'MPSC or Exception'");
//                    });


//                RuleFor(req => req.EndDate)
//                   .GreaterThan(req => req.StartDate)
//                   .WithMessage("EndDate must be greater than StartDate.");



//                RuleForEach(req => req.Feeders)
//                  .ChildRules(feeders =>
//                  {
//                      feeders.RuleFor(f => f.Data.Parent)
//                          .NotEmpty()
//                          .WithMessage("Parent in Feeders must be a valid GUID.");
//                  });


//                RuleForEach(req => req.CustomParameterIds)
//                    .ChildRules(customParams =>
//                    {
//                        customParams.RuleForEach(cp => cp.Value.ParameterList)
//                            .ChildRules(parameterList =>
//                            {
//                                parameterList.RuleFor(p => p.Quantity)
//                                    .Must(q => q == "QSAMPLE" || q == "AVG" || q == "QAVG")
//                                    .WithMessage("Quantity must be either 'QSAMPLE' or 'AVG'.");
//                            });
//                    });

//            }
//        }

//        private readonly ILogger<PQSTrendDataValidationService> _logger;

//        public PQSTrendDataValidationService(ILogger<PQSTrendDataValidationService> logger)
//        {
//            _logger = logger;
//        }

//        public async Task RegistrationRequestValidateAsync(CalcRequestDto request)
//        {
//            var validator = new CalcRequestDtoValidator();
//            var validationResult = validator.Validate(request);

//            var errors = Dissect(validationResult);

//            Validate(errors);
//        }
//    }
//}
