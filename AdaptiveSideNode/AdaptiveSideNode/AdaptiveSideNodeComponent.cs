using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace MeroConnection01
{
    public class MeroConnection01Component : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public MeroConnection01Component()
          : base("MeroConnection01", "Nickname",
              "Description",
              "Extra", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("CenPt", "CenPt", "CenPt", GH_ParamAccess.item);
            pManager.AddVectorParameter("CenVec", "CenVec", "CenVec", GH_ParamAccess.item);
            pManager.AddLineParameter("BAxisList", "BAxisList", "BAxisList", GH_ParamAccess.list);
            pManager.AddNumberParameter("RingRad", "RingRad", "RingRad", GH_ParamAccess.item);
            pManager.AddNumberParameter("ArmWidth", "ArmWidth", "ArmWidth", GH_ParamAccess.item);
            pManager.AddNumberParameter("ArmStDis", "ArmStDis", "ArmStDis", GH_ParamAccess.item);
            pManager.AddNumberParameter("NodeHeight", "NodeHeight", "NodeHeight", GH_ParamAccess.item);
            pManager.AddNumberParameter("HoleRad", "HoleRad", "HoleRad", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Ring2D", "Ring2D", "Ring2D", GH_ParamAccess.list);
            pManager.AddCurveParameter("NGon", "NGon", "NGon", GH_ParamAccess.item);
            pManager.AddBrepParameter("NGon3D", "NGon3D", "NGon3D", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d CenPt = Point3d.Unset;
            Vector3d Normal = Vector3d.Unset;
            List<Curve> CrvCen = new List<Curve>();
            //Curve CentralCrv = CrvCen[0];
            List<Line> BeamAxis = new List<Line>();
            double RingRad = double.NaN;
            double ArmWid = double.NaN;
            double ArmStDis = double.NaN;
            double Height = double.NaN;
            double HoleRad = double.NaN;

            if (!DA.GetData(0, ref CenPt)) { return; }
            if (!DA.GetData(1, ref Normal)) { return; }
            if (!DA.GetDataList(2, BeamAxis)) { return; }
            if (!DA.GetData(3, ref RingRad)) { return; }
            if (!DA.GetData(4, ref ArmWid)) { return; }
            if (!DA.GetData(5, ref ArmStDis)) { return; }
            if (!DA.GetData(6, ref Height)) { return; }
            if (!DA.GetData(7, ref HoleRad)) { return; }


            //Instantiate the Class
            MeroConnection01 Frst2d = new MeroConnection01();

            //CollectLinesInArrays
            List<Line> GroupLines = new List<Line>();
            foreach (Line li in BeamAxis)
            {
                if (CenPt.DistanceTo(li.PointAt(0)) <= Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) { GroupLines.Add(li); }
                if (CenPt.DistanceTo(li.PointAt(1)) <= Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance) { GroupLines.Add(li); }
            }

            List<Line> connlines = GroupLines;


            List<Line> CorrLines = new List<Line>();
            foreach (Line i in GroupLines)
            {
                Line Corr = Frst2d.CorrectLineDirection(CenPt, i);
                CorrLines.Add(Corr);
            }

            //CreateCentralCurve
            Plane Orientation = new Plane(CenPt, Normal);
            Circle CentralCurve = new Circle(Orientation, RingRad);
            Curve CenCrv = CentralCurve.ToNurbsCurve();

            List<Curve> SideLines = new List<Curve>();


            for (int li = 0; li < CorrLines.Count; li++)
            {
                Line Sidei = Frst2d.CreateSideTypeCen(CenPt, CorrLines[li], Normal, ArmWid, RingRad, ArmStDis);
                Curve Sidecrv = Sidei.ToNurbsCurve();
                SideLines.Add(Sidecrv);
            }

            //CreateNgoneDetail
            List<Line> Sides;
            Curve NgoneMero = Frst2d.NGoneMero(CenPt, CenCrv, CorrLines, Normal, ArmWid, RingRad, 10, out Sides);

            //Create3DNgone
            Brep Ngone3D = Frst2d.NGoneMero3D(CenPt, Normal, NgoneMero, Height, HoleRad);

            DA.SetDataList(0, SideLines);

            DA.SetData(1, NgoneMero);

            DA.SetData(2, Ngone3D);

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{d6a0f6fd-f66c-44d4-8003-fab32aff97e6}"); }
        }
    }
}
