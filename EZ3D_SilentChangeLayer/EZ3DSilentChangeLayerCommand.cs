using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace EZ3D_SilentChangeLayer
{
    [System.Runtime.InteropServices.Guid("8e7f3500-1ea3-4e46-9e23-4511062b03b6")]
    public class EZ3DSilentChangeLayerCommand : Command
    {
        public EZ3DSilentChangeLayerCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static EZ3DSilentChangeLayerCommand Instance
        {
            get;
            private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "EZ3DSilentChangeLayerCommand"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            int destLayerIndex;

/*
            foreach (Rhino.DocObjects.Layer layer in doc.Layers)
            {
                RhinoApp.WriteLine(layer.LayerIndex.ToString() + ")(" + layer.Name + ":" + layer.ToString());
            }
 */
            using (GetInteger getInt = new GetInteger())
            {
                getInt.SetCommandPrompt("Enter new default Layer Index");
                if (getInt.Get() != GetResult.Number)
                {
                    RhinoApp.WriteLine("No layer index recieved.");
                    return Result.Failure;
                }
                destLayerIndex = getInt.Number();
            }

            doc.Layers.SetCurrentLayerIndex(destLayerIndex,false);
            return Result.Success;
        }
    }
}
