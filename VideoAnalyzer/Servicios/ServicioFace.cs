using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Cognitive.Face;
using VideoAnalyzer.Helpers;
using Xamarin.Cognitive.Face.Model;
using System.IO;

namespace VideoAnalyzer.Servicios
{
    public static class ServicioFace
    {
        public static async Task<Face> DetectarRostro(byte[] foto)
        {
            FaceClient.Shared.Endpoint = Constantes.FaceApiURL;
            FaceClient.Shared.SubscriptionKey = Constantes.FaceApiKey;

            try
            {
                if (foto != null)
                {
                    var atributosFace = new FaceAttributeType[] { FaceAttributeType.Age, FaceAttributeType.Gender, FaceAttributeType.HeadPose, FaceAttributeType.Emotion };

                    using (var stream = new MemoryStream(foto))
                    {
                        var rostros = await FaceClient.Shared.DetectFacesInPhoto(stream, true, atributosFace);
                        if (rostros.Count > 0)
                            return rostros.FirstOrDefault();
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }
    }
}