using Android.App;
using Android.Widget;
using Android.OS;
using Android.Hardware;
using VideoAnalyzer.Helpers;
using System;
using static Android.Hardware.Camera;
using Java.IO;
using Java.Text;
using System.Timers;
using Java.Util;
using static Android.Views.ViewGroup;
using VideoAnalyzer.Servicios;
using Android.Media;
using System.Threading.Tasks;

namespace VideoAnalyzer
{
    [Activity(Label = "VideoAnalyzer", MainLauncher = true)]
    public class MainActivity : Activity, Android.Hardware.Camera.IPictureCallback
    {
        private Camera mCamera;
        private CameraPreview mCameraPreview;
        private bool isProcessStarted = false;
        private bool isAnalyzing = false;

        private MediaPlayer player;
        private bool isPlaying;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Main);
            TableLayout tl = FindViewById<TableLayout>(Resource.Id.camera_preview);
            LayoutParams layoutParams = new TableRow.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);
            TableRow.LayoutParams tableLayoutParams = new TableRow.LayoutParams();
            tableLayoutParams.Span = 4;
            tableLayoutParams.Weight = 1;
            TableRow tr = new TableRow(this);

            mCamera = getCameraInstance();
            mCameraPreview = new CameraPreview(this, mCamera, null, Android.Views.SurfaceOrientation.Rotation0);
            mCameraPreview.LayoutParameters = tableLayoutParams;
            tr.AddView(mCameraPreview, 0);
            tl.AddView(tr, 6);

            Button captureButton = FindViewById<Button>(Resource.Id.button_capture);
            captureButton.Click += CaptureButton_Click;
        }

        private void CaptureButton_Click(object sender, System.EventArgs e)
        {
            if (!isProcessStarted)
            {
                isProcessStarted = true;
                Toast.MakeText(this, "Comienza el proceso", ToastLength.Short).Show();
                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Interval = 10000;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                if (!isAnalyzing)
                {
                    isAnalyzing = true;
                    mCamera.StartPreview();
                    mCamera.TakePicture(null, null, this);
                    Toast.MakeText(this, "Foto tomada", ToastLength.Short).Show();
                }
            });
        }

        private Camera getCameraInstance()
        {
            Camera camera = null;
            try
            {
                int cameraId = -1;
                // Search for the front facing camera
                int numberOfCameras = Camera.NumberOfCameras;
                for (int i = 0; i < numberOfCameras; i++)
                {
                    CameraInfo info = new CameraInfo();
                    Camera.GetCameraInfo(i, info);
                    if (info.Facing == CameraInfo.CameraFacingFront)
                    {
                        cameraId = i;
                        break;
                    }
                }

                camera = Camera.Open(cameraId);
            }
            catch (Exception e)
            {
                // cannot get camera or does not exist
            }
            return camera;
        }

        IPictureCallback mPicture;

        public static Android.Graphics.Bitmap rotateImage(Android.Graphics.Bitmap source, float angle)
        {
            Android.Graphics.Matrix matrix = new Android.Graphics.Matrix();
            matrix.PostRotate(angle);
            return Android.Graphics.Bitmap.CreateBitmap(source, 0, 0, source.Width, source.Height, matrix, true);
        }

        public static byte[] bitmaptoByte(Android.Graphics.Bitmap bitmap)
        {
            var baos = new System.IO.MemoryStream();
            bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, 90, baos);
            byte[] data = baos.ToArray();
            return data;
        }

        public static Android.Graphics.Bitmap LoadFromFile(String filename)
        {
            try
            {
                File f = new File(filename);
                if (!f.Exists()) { return null; }
                Android.Graphics.Bitmap tmp = Android.Graphics.BitmapFactory.DecodeFile(filename);
                return tmp;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        async void Camera.IPictureCallback.OnPictureTaken(byte[] data, Android.Hardware.Camera camera)
        {
            File pictureFile = getOutputMediaFile();

            if (pictureFile == null)
            {
                return;
            }

            try
            {
                FileOutputStream fos = new FileOutputStream(pictureFile);
                fos.Write(data);
                fos.Close();

                ExifInterface ei = new ExifInterface(pictureFile.AbsolutePath);
                var orientation = (Android.Media.Orientation)ei.GetAttributeInt(ExifInterface.TagOrientation, (int)Android.Media.Orientation.Undefined);

                Android.Graphics.Bitmap rotatedBitmap = null;
                Android.Graphics.Bitmap bitmap = LoadFromFile(pictureFile.AbsolutePath);

                switch (orientation)
                {
                    case Android.Media.Orientation.Rotate90:
                        rotatedBitmap = rotateImage(bitmap, 0);
                        break;

                    case Android.Media.Orientation.Rotate180:
                        rotatedBitmap = rotateImage(bitmap, 90);
                        break;

                    case Android.Media.Orientation.Rotate270:
                        rotatedBitmap = rotateImage(bitmap, 180);
                        break;

                    case Android.Media.Orientation.Normal:
                    default:
                        rotatedBitmap = rotateImage(bitmap, 270);
                        break;
                }

                var data2 = bitmaptoByte(rotatedBitmap);
                FileOutputStream fos2 = new FileOutputStream(pictureFile);
                fos2.Write(data2);
                fos2.Close();

                var rostro = await ServicioFace.DetectarRostro(data2);
                var descripcion = await ServicioVision.DescribirImagen(data2);

                if (rostro != null)
                {
                    RunOnUiThread(async () =>
                    {
                        var frente = ImageAnalyzer.AnalizarPostura(rostro);
                        TextView txtFrente = FindViewById<TextView>(Resource.Id.txtFrente);
                        TextView txtAnalisisFrente = FindViewById<TextView>(Resource.Id.txtAnalisisFrente);
                        txtFrente.Text = frente.ToString("N2");
                        if (frente > Constantes.LookingAwayAngleThreshold)
                        {
                            txtFrente.SetTextColor(Android.Graphics.Color.Red);
                            txtAnalisisFrente.SetTextColor(Android.Graphics.Color.Red);
                            txtAnalisisFrente.Text = "No estás mirando al frente";

                            await PlayAlarm();
                        }
                        else
                        {
                            txtFrente.SetTextColor(Android.Graphics.Color.Green);
                            txtAnalisisFrente.SetTextColor(Android.Graphics.Color.Green);
                            txtAnalisisFrente.Text = "OK";
                        }

                        var boca = ImageAnalyzer.AnalizarBoca(rostro);
                        TextView txtBoca = FindViewById<TextView>(Resource.Id.txtBoca);
                        TextView txtAnalisisBoca = FindViewById<TextView>(Resource.Id.txtAnalisisBoca);
                        txtBoca.Text = boca.ToString("N2");

                        if (boca > Constantes.YawningApertureThreshold)
                        {
                            txtBoca.SetTextColor(Android.Graphics.Color.Red);
                            txtAnalisisBoca.SetTextColor(Android.Graphics.Color.Red);
                            txtAnalisisBoca.Text = "Posiblemente está bostezando";

                            await PlayAlarm();
                        }
                        else
                        {
                            txtBoca.SetTextColor(Android.Graphics.Color.Green);
                            txtAnalisisBoca.SetTextColor(Android.Graphics.Color.Green);
                            txtAnalisisBoca.Text = "OK";
                        }

                        var ojos = ImageAnalyzer.AnalizarOjos(rostro);
                        TextView txtOjos = FindViewById<TextView>(Resource.Id.txtOjos);
                        TextView txtAnalisisOjos = FindViewById<TextView>(Resource.Id.txtAnalisisOjos);
                        txtOjos.Text = ojos.ToString("N2");

                        if (ojos < Constantes.SleepingApertureThreshold)
                        {
                            txtOjos.SetTextColor(Android.Graphics.Color.Red);
                            txtAnalisisOjos.SetTextColor(Android.Graphics.Color.Red);
                            txtAnalisisOjos.Text = "¡Está dormido!";

                            await PlayAlarm();
                        }
                        else
                        {
                            txtOjos.SetTextColor(Android.Graphics.Color.Green);
                            txtAnalisisOjos.SetTextColor(Android.Graphics.Color.Green);
                            txtAnalisisOjos.Text = "OK";
                        }

                        if (descripcion.Description.Captions.Length > 0)
                        {
                            var distraccion = descripcion.Description.Captions[0].Text;
                            TextView txtCelular = FindViewById<TextView>(Resource.Id.txtCelular);
                            TextView txtAnalisisCelular = FindViewById<TextView>(Resource.Id.txtAnalisisCelular);

                            if (distraccion.Contains("phone"))
                            {
                                txtCelular.Text = "SI";
                                txtCelular.SetTextColor(Android.Graphics.Color.Red);
                                txtAnalisisCelular.SetTextColor(Android.Graphics.Color.Red);
                                txtAnalisisCelular.Text = "¡Está usando el teléfono móvil!";
                            }
                            else
                            {
                                txtCelular.Text = "NO";
                                txtCelular.SetTextColor(Android.Graphics.Color.Green);
                                txtAnalisisCelular.SetTextColor(Android.Graphics.Color.Green);
                                txtAnalisisCelular.Text = "OK";
                            }
                        }
                    });
                }
            }
            catch (FileNotFoundException e)
            {

            }
            catch (IOException e)
            {
            }
            finally
            {
                isAnalyzing = false;
            }
        }

        private static File getOutputMediaFile()
        {
            File mediaStorageDir = new File(
                    Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures),
                    "VideoAnalyzer");
            if (!mediaStorageDir.Exists())
            {
                if (!mediaStorageDir.Mkdirs())
                {
                    //Log.d("MyCameraApp", "failed to create directory");
                    return null;
                }
            }
            // Create a media file name
            String timeStamp = new SimpleDateFormat("yyyyMMdd_HHmmss")
                    .Format(new Date());
            File mediaFile;
            mediaFile = new File(mediaStorageDir.Path + File.Separator
                    + "IMG_" + timeStamp + ".jpg");

            return mediaFile;
        }


        public async Task<bool> PlayAlarm()
        {
            if (!isPlaying)
            {
                isPlaying = true;

                //for (int i = 0; i < 3; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    if (player != null)
                    {
                        player.Stop();
                        player.Release();
                        player = null;
                    }

                    player = MediaPlayer.Create(this, Resource.Raw.beep);
                    player.Start();
                }

                isPlaying = false;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}