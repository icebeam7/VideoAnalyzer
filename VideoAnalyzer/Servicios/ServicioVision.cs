using System;
using System.Threading.Tasks;
using VideoAnalyzer.Helpers;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System.IO;

namespace VideoAnalyzer.Servicios
{
    public static class ServicioVision
    {
        public async static Task<AnalysisResult> DescribirImagen(byte[] foto)
        {
            AnalysisResult analisis = null;

            try
            {
                if (foto != null)
                {
                    using (var stream = new MemoryStream(foto))
                    {
                        var clienteVision = new VisionServiceClient(Constantes.VisionApiKey, Constantes.VisionApiURL);
                        analisis = await clienteVision.DescribeAsync(stream);
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return analisis;
        }
    }
}
