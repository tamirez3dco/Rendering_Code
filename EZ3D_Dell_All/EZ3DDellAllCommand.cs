using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.DocObjects;

namespace EZ3D_Dell_All
{
    [System.Runtime.InteropServices.Guid("68cfbe25-8d86-49d3-b0c7-9e6550e38f6c")]
    public class EZ3DDellAllCommand : Command
    {
        public EZ3DDellAllCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static EZ3DDellAllCommand Instance
        {
            get;
            private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "EZ3DDellAllCommand"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            List<Guid> geometryObjects_for_deletion = new List<Guid>();
            foreach (Rhino.DocObjects.Layer layer in doc.Layers)
            {
                if (layer.Name == "lights") continue;
                if (layer.Name == "Objects")
                {
                    RhinoApp.WriteLine("Giving up on Layer Objects");
                    continue;
                }
                RhinoApp.WriteLine(layer.LayerIndex.ToString() + ")(" + layer.Name + ":" + layer.ToString());
                RhinoObject[] rhobjs = doc.Objects.FindByLayer(layer.Name);
                foreach (RhinoObject robj in rhobjs)
                {
                    if (robj.ObjectType == ObjectType.Light) continue;
                    geometryObjects_for_deletion.Add(robj.Id);
                }
            }

            doc.Objects.Delete(geometryObjects_for_deletion,true);

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
