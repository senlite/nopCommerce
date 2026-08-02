using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace TwinParticles.CheckEngine.Models;

public record ConfigurationModel : BaseNopModel
{
    [NopResourceDisplayName("Plugins.TwinParticles.CheckEngine.Configuration.Fields.Enabled")]
    public bool Enabled { get; set; }
}
