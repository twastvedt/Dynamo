using Dynamo.Controls;
using Dynamo.Wpf;
using Watch3DNodeModels;

namespace Watch3DNodeModelsWpf
{
    public class HydraWatch3DNodeViewCustomization : Watch3DNodeViewCustomization, INodeViewCustomization<HydraWatch3D>
    {
        public void CustomizeView(HydraWatch3D model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);
        }
    }
}
