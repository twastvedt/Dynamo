using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

using Autodesk.DesignScript.Interfaces;

using Dynamo.Controls;
using Dynamo.Visualization;
using Dynamo.Wpf;
using VMDataBridge;

using Watch3DNodeModels;

namespace Watch3DNodeModelsWpf
{
    public class HydraWatch3DNodeViewCustomization : Watch3DNodeViewCustomization, INodeViewCustomization<HydraWatch3D>
    {
        public void CustomizeView(HydraWatch3D model, NodeView nodeView)
        {
            base.CustomizeView(model, nodeView);

            // Override the Helix callback with Hydra.
            DataBridge.Instance.RegisterCallback(
                model.GUID.ToString(),
                obj =>
                    nodeView.Dispatcher.Invoke(
                        new Action<object>(RenderData),
                        DispatcherPriority.Render,
                        obj));
        }

        private void RenderData(object data)
        {
            IEnumerable<IGraphicItem> items = UnpackRenderData(data);

            watch3DViewModel.AddGeometryForRenderPackages(new RenderPackageCache(packages));
        }
    }
}
