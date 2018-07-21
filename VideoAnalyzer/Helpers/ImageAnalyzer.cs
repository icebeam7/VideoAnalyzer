using System;
using Xamarin.Cognitive.Face.Model;

namespace VideoAnalyzer.Helpers
{
    public static class ImageAnalyzer
    {
        public static double AnalizarPostura(Face f)
        {
            double headPoseDeviation = Math.Abs(f.Attributes.HeadPose.Yaw);
            double deviationRatio = f.Attributes.HeadPose.Yaw / 35;

            return headPoseDeviation;
        }

        public static double AnalizarBoca(Face f)
        {
            double mouthWidth = Math.Abs(f.Landmarks.MouthRight.X - f.Landmarks.MouthLeft.X);
            double mouthHeight = Math.Abs(f.Landmarks.UpperLipBottom.Y - f.Landmarks.UnderLipTop.Y);

            double mouthAperture = mouthHeight / mouthWidth;
            mouthAperture = Math.Min((mouthAperture - 0.1) / 0.4, 1);

            return mouthAperture;
        }

        public static double AnalizarOjos(Face f)
        {
            double leftEyeWidth = Math.Abs(f.Landmarks.EyeLeftInner.X - f.Landmarks.EyeLeftOuter.X);
            double leftEyeHeight = Math.Abs(f.Landmarks.EyeLeftBottom.Y - f.Landmarks.EyeLeftTop.Y);

            double rightEyeWidth = Math.Abs(f.Landmarks.EyeRightInner.X - f.Landmarks.EyeRightOuter.X);
            double rightEyeHeight = Math.Abs(f.Landmarks.EyeRightBottom.Y - f.Landmarks.EyeRightTop.Y);

            double eyeAperture = Math.Max(leftEyeHeight / leftEyeWidth, rightEyeHeight / rightEyeWidth);
            eyeAperture = Math.Min((eyeAperture - 0.2) / 0.3, 1);

            return eyeAperture;
        }
    }
}