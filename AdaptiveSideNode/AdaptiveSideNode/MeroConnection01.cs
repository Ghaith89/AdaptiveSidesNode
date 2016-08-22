using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace MeroConnection01
{
    class MeroConnection01
    {
        public Line CreateSideTypeCen(Point3d Cen, Line SpAxe, Vector3d Normal, double Width, double Rad, double ArmTolerance)
        {

            //double ArmTolerance = 0.2;
            Point3d Stpt = SpAxe.PointAt(0);
            Point3d Edpt = SpAxe.PointAt(1);
            Vector3d LiVec = Edpt - Stpt;

            LiVec.Unitize();
            Plane Base = new Rhino.Geometry.Plane(Cen, Normal);
            Transform Project = Transform.PlanarProjection(Base);
            Curve PrLine = SpAxe.ToNurbsCurve();
            PrLine.Transform(Project);
            LiVec.Transform(Project);

            //PrLine.PointAt(0);
            Point3d PrEdPt = PrLine.PointAt(1);
            Point3d PrStPt = PrLine.PointAt(0);
            PrStPt.Transform(Project);
            Vector3d VecWidth = Vector3d.CrossProduct(Normal, LiVec);
            VecWidth.Unitize();

            double HalWid = Width / 2;

            Transform SiWidth = Transform.Translation(VecWidth * HalWid);
            Transform NegWidth = Transform.Translation(VecWidth * -1 * HalWid);

            Point3d Popos = new Point3d(PrStPt);
            Popos.Transform(SiWidth);

            //Subtracted Rectangle
            Point3d PoNeg = new Point3d(PrStPt);
            PoNeg.Transform(NegWidth);
            Line Side0 = new Line(Popos, PoNeg);
            Curve side = Side0.ToNurbsCurve();
            Line side011 = new Line(Popos, PoNeg);
            Curve side01 = side011.ToNurbsCurve();
            double RingArmRad = Rad + ArmTolerance;
            LiVec.Unitize();
            Transform LivecTr = Transform.Translation(LiVec * (Rad + ArmTolerance));
            side011.Transform(LivecTr);


            return side011;
        }

        public Curve NGoneMero(Point3d Cen, Curve CenCrv, List<Line> SpAxes, Vector3d Normal, double Wid, double Rad, double ArmTolerance, out List<Line> Sides)
        {
            //Creat The Sides

            Sides = new List<Line>();
            List<Point3d> EndPts = new List<Point3d>();

            foreach (Line li in SpAxes)
            {
                //double sidewid = Rad * 20;
                Line Side = CreateSideTypeCen(Cen, li, Normal, Wid, Rad, 10);
                Point3d po1 = Side.PointAt(0);
                EndPts.Add(po1);
                Point3d po2 = Side.PointAt(1);
                EndPts.Add(po2);
                Sides.Add(Side);

            }

            Dictionary<double, Point3d> EvaLine = new Dictionary<double, Point3d>();
            foreach (Point3d i in EndPts)
            {
                double t;
                Transform Sc = Transform.Scale(Cen, 10);
                CenCrv.Transform(Sc);
                CenCrv.ClosestPoint(i, out t);
                EvaLine.Add(t, i);

            }
            var Sorted = from pair in EvaLine orderby pair.Key ascending select pair;

            List<Point3d> SortedPts = new List<Point3d>();
            foreach (KeyValuePair<double, Point3d> pair in Sorted)
            {
                SortedPts.Add(pair.Value);
            }
            SortedPts.Add(SortedPts[0]);

            Polyline PolyLine = new Polyline(SortedPts);
            Curve Ngone = PolyLine.ToNurbsCurve();

            /*for (int i=0; i< SortedLines.Count-1; i++)
            {
                Line OpLine = SortedLines[i];
                Line TrLine01 = SortedLines[i + 1];
                Line TrLine02 = SortedLines[SortedLines.Count-2+i];
                double a0;
                double b0;

                Rhino.Geometry.Intersect.Intersection.LineLine(OpLine, TrLine01, out a0, out b0);
                Point3d po0 = OpLine.PointAt(a0);
                double a1;
                double b1;
                Rhino.Geometry.Intersect.Intersection.LineLine(OpLine, TrLine02, out a1, out b1);
                Point3d po1 = OpLine.PointAt(a1);

                Line FinalSide = new Line(po0, po1);
                Curve FinalSideCrv = FinalSide.ToNurbsCurve();
                FinalSides.Add(FinalSideCrv);
            }

            Curve[] NGoneList =  Curve.JoinCurves(FinalSides, 10);
            Curve Ngone = NGoneList[0];*/

            return Ngone;
        }

        public Brep NGoneMero3D(Point3d Cen, Vector3d Normal, Curve Ngone, double Height, double HoleRad)
        {
            double HalHeight = Height / 2;
            List<Brep> SurfParts = new List<Brep>();
            Normal.Unitize();
            Surface FstHalf = Surface.CreateExtrusion(Ngone, Normal * HalHeight);
            Brep FstHalfBrep = FstHalf.ToBrep();
            SurfParts.Add(FstHalfBrep);
            Surface SndHalf = Surface.CreateExtrusion(Ngone, -Normal * HalHeight);
            Brep SndHalfBrep = SndHalf.ToBrep();
            SurfParts.Add(SndHalfBrep);
            Brep[] Connected = Brep.JoinBreps(SurfParts, 10);
            Brep SideWalls = Connected[0];
            Brep Caped = SideWalls.CapPlanarHoles(10);
            //Create Hole
            Plane Base = new Plane(Cen, Normal);
            Circle CenCir = new Circle(Base, HoleRad);
            Curve CenCirCrv = CenCir.ToNurbsCurve();
            List<Brep> HoleBreps = new List<Brep>();
            Surface Holewalls0 = Surface.CreateExtrusion(CenCirCrv, Normal * Height);
            Brep Hole0 = Holewalls0.ToBrep();
            HoleBreps.Add(Hole0);
            Surface Holewalls1 = Surface.CreateExtrusion(CenCirCrv, -Normal * Height);
            Brep Hole1 = Holewalls1.ToBrep();
            HoleBreps.Add(Hole1);
            Brep[] ConnectedHole = Brep.JoinBreps(HoleBreps, 10);
            Brep Hole = ConnectedHole[0];
            Brep CapedHole = Hole.CapPlanarHoles(10);
            Brep[] FinalNgones = Brep.CreateBooleanDifference(Caped, CapedHole, 10);
            Brep FinalNgone = FinalNgones[0];

            return FinalNgone;
        }

        public Line CorrectLineDirection(Point3d cen, Line Axe)
        {
            Line Corr;
            if (cen.DistanceTo(Axe.PointAt(1)) <= Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
            {
                Corr = Axe;
                Corr.Flip();
            }
            else { Corr = Axe; }
            return Corr;
        }

        public Vector3d NormalFromConnectedlines(Point3d Cen, List<Line> ConnectedLines)
        {
            List<Vector3d> LiVecs = new List<Vector3d>();
            List<Vector3d> PerpVecs = new List<Vector3d>();
            List<Line> CoLines = ConnectedLines;
            Line EdLine = ConnectedLines[0];
            CoLines.Add(EdLine);
            foreach (Line li in CoLines)
            {
                Line CorrLi = CorrectLineDirection(Cen, li);
                Vector3d LiVec = CorrLi.PointAt(1) - CorrLi.PointAt(0);
                LiVec.Unitize();

                LiVecs.Add(LiVec);
            }
            for (int i = 0; i < LiVecs.Count - 1; i++)
            {
                Vector3d Perp = Vector3d.CrossProduct(LiVecs[i], LiVecs[i + 1]);
                Perp.Unitize();
                PerpVecs.Add(Perp);
            }
            Vector3d AvVec = Ave(PerpVecs);

            return AvVec;
        }

        public Vector3d Ave(List<Vector3d> Vectors)
        {
            Vector3d result = new Vector3d(0, 0, 0);

            for (int i = 0; i < Vectors.Count; i++)
            {
                result += Vectors[i];
            }

            return result / Vectors.Count;
        }

    }


}
