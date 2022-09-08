using Dynamo.Graph.Nodes;
using Watch3DNodeModels.Properties;

namespace Watch3DNodeModels
{
    [NodeName("Watch 3D: Hydra")]
    [NodeCategory(BuiltinNodeCategories.CORE_VIEW)]
    [NodeDescription("Watch3DDescription", typeof(Resources))]
    [OutPortTypes("var")]
    [IsDesignScriptCompatible]
    public class HydraWatch3D : Watch3D
    {
    }
}
