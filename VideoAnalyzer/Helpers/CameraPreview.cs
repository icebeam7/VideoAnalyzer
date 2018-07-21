using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Java.IO;
using Android.Hardware;

namespace VideoAnalyzer.Helpers
{
    public class CameraPreview : SurfaceView, ISurfaceHolderCallback
    {
        private ISurfaceHolder mSurfaceHolder;
        private Camera mCamera;

        public CameraPreview(Context context, Camera camera, Camera.CameraInfo cameraInfo,
            SurfaceOrientation displayOrientation) :
            base(context)
        {
            this.mCamera = camera;
            this.mSurfaceHolder = this.Holder;
            this.mSurfaceHolder.AddCallback(this);
            this.mSurfaceHolder.SetType(SurfaceType.PushBuffers);
        }

        public void SurfaceChanged(ISurfaceHolder holder, Android.Graphics.Format format, int w, int h)
        {
            // start preview with new settings
            try
            {
                mCamera.SetPreviewDisplay(holder);
                mCamera.StartPreview();
            }
            catch (Exception e)
            {
                // intentionally left blank for a test
            }
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            try
            {
                mCamera.SetPreviewDisplay(holder);
                mCamera.SetDisplayOrientation(90);
                mCamera.StartPreview();
            }
            catch (IOException e)
            {
                // left blank for now
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            mCamera.StopPreview();
            mCamera.Release();
        }
    }
}