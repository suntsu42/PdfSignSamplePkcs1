using iText.Kernel.Pdf;
using iText.Signatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfSignSamplePkcs1
{
    /// <summary>
    /// This is a dummy implementation of the IExternalSignatureContainer
    /// It is only used to prepare the pdf (reserve space) and to create the hash(Digest) of the pdf
    /// </summary>
    internal class DigestCalcBlankSigner : IExternalSignatureContainer
    {
        private readonly PdfName _filter;

        private readonly PdfName _subFilter;

        public byte[] PdfDigest { get; set; }


        internal DigestCalcBlankSigner(PdfName filter, PdfName subFilter)
        {
            _filter = filter;
            _subFilter = subFilter;
        }

        /// <summary>
        /// This Sign method is called internally during iText SignExternalContainer() method
        /// Usually, the signature would be returned here. But since we only need the hash we just calculate the current hash 
        /// and set it as a variable. It must be read after the SignExternalContainer() call
        /// We then return a 0 byte signature which is applied as a placeholder
        /// </summary>
        /// <param name="data">document data</param>
        /// <returns></returns>
        public virtual byte[] Sign(Stream data)
        {
            PdfDigest = DigestAlgorithms.Digest(data, DigestAlgorithms.GetMessageDigest("SHA256"));
            return new byte[0];
        }

        /// <summary>
        /// We need to set PdfName.Adobe_PPKLite, PdfName.Adbe_pkcs7_detached which are supplied in the constructor
        /// </summary>
        /// <param name="signDic">The PdfDictionary to chich the filters must be applied</param>
        public virtual void ModifySigningDictionary(PdfDictionary signDic)
        {
            signDic.Put(PdfName.Filter, _filter);
            signDic.Put(PdfName.SubFilter, _subFilter);
        }


    }
}
