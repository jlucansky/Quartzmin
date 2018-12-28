using System.Collections.Generic;

namespace Quartzmin.Models
{
    public interface IHasValidation
    {
        void Validate(ICollection<ValidationError> errors);
    }
}