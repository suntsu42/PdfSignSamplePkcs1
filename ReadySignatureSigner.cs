using iText.Kernel.Pdf;
using iText.Signatures;
using System.IO;

namespace PdfSignSamplePkcs1
{
    /// <summary>
    /// This implementation of IExternalSignatureContainer is used to apply the signature to the prepared pdf document
    /// </summary>
    internal class ReadySignatureSigner : IExternalSignatureContainer
    {
        private readonly byte[] cmsSignatureContents;

        /// <summary>
        /// ctor which sets the CMS to be applied in the sign operation
        /// </summary>
        /// <param name="cmsSignatureContents"></param>
        internal ReadySignatureSigner(byte[] cmsSignatureContents)
        {
            this.cmsSignatureContents = cmsSignatureContents;
        }

        /// <summary>
        /// This Sign method is called internally during iText SignExternalContainer() method call
        /// Here we just return the pkcs7 CMS which was set in the ctor and already contains the signature
        /// </summary>
        /// <param name="data">the data to sign. This is not required</param>
        /// <returns></returns>
        public virtual byte[] Sign(Stream data)
        {
            return cmsSignatureContents;
        }

        public virtual void ModifySigningDictionary(PdfDictionary signDic)
        {
        }
    }
}
