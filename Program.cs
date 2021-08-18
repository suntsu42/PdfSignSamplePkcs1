using iText.Kernel.Pdf;
using iText.Signatures;
using System;
using System.IO;

namespace PdfSignSamplePkcs1
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO >> Change this path based on your local system
            var resourcePath = @"c:\project\github\PdfSignSamplePkcs1\Resources\";
            var pdfToSignPath = Path.Combine(resourcePath, "test.pdf");
            var signedPdfPath = Path.Combine(resourcePath, "signedPdf.pdf");
            var privateKey = Path.Combine(resourcePath, "SignTest.pfx"); // not critical, self signed certificate
            var privateKeyPassword = "test";

            // ############
            // Change value in order to create the PKCS7
            // either manually or via Itext
            // ############
            bool createSignatureViaPlainPkcs1 = false;

            //delete signed file if it exists
            if (System.IO.File.Exists(signedPdfPath))
                System.IO.File.Delete(signedPdfPath);
            var pdfToSign = System.IO.File.ReadAllBytes(pdfToSignPath);
            byte[] pdfDigest = null;


            //#1 Prepare pdf for signing
            var SignatureAttributeName = $"SignatureAttributeName_{DateTime.Now:yyyyMMddTHHmmss}";
            byte[] preparedToSignPdf = null;
            using (MemoryStream input = new MemoryStream(pdfToSign))
            {
                using (var reader = new PdfReader(input))
                {
                    StampingProperties sp = new StampingProperties();
                    sp.UseAppendMode();
                    using (MemoryStream baos = new MemoryStream())
                    {
                        var signer = new PdfSigner(reader, baos, sp);
                        signer.SetCertificationLevel(PdfSigner.NOT_CERTIFIED);

                        signer.SetFieldName(SignatureAttributeName);

                        DigestCalcBlankSigner external = new DigestCalcBlankSigner(PdfName.Adobe_PPKLite, PdfName.Adbe_pkcs7_detached);
                        signer.SignExternalContainer(external, 32000);

                        //get digest to be signed
                        pdfDigest = external.PdfDigest;
                        preparedToSignPdf = baos.ToArray();
                    }
                }
            }

            //#2 Create PKCS7
            SignService ss = new SignService(pdfDigest, privateKey, privateKeyPassword);
            byte[] signatureAsPkcs7 = null;
            if (createSignatureViaPlainPkcs1)
                signatureAsPkcs7 = ss.CreatePKCS7ViaPkcs1(); // >> Creates invalid pdf signature
            else
                signatureAsPkcs7 = ss.CreatePKCS7(); // Creates valid pdf signature

            //#3 apply cms(PKCS7) to prepared pdf
            ReadySignatureSigner extSigContainer = new ReadySignatureSigner(signatureAsPkcs7);
            using (MemoryStream preparedPdfStream = new MemoryStream(preparedToSignPdf))
            {
                using (var pdfReader = new PdfReader(preparedPdfStream))
                {
                    using (PdfDocument docToSign = new PdfDocument(pdfReader))
                    {
                        using (MemoryStream outStream = new MemoryStream())
                        {
                            PdfSigner.SignDeferred(docToSign, SignatureAttributeName, outStream, extSigContainer);
                            System.IO.File.WriteAllBytes(signedPdfPath, outStream.ToArray());
                        }
                    }
                }
            }
        }
    }


}
