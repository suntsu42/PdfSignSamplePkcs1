# Overview

This project is a sample application for signing pdf documents using iText. It is used to demonstrate a problem when creating the signature manually as PKCS1. 

# Stackoverflow

I want to digitally sign pdf documents using iText 7. The signature is created by an external service which returns a PKCS1 signature only. I then have to create and apply  the PKCS7. 

There is a good documentation for this scenario from iText: https://kb.itextpdf.com/home/it7kb/examples/how-to-use-a-digital-signing-service-dss-such-as-globalsign-with-itext-7

I have created a sample application which signs pdf documents via local certificate. This sample application can be cloned from https://github.com/suntsu42/PdfSignSamplePkcs1. In this sample application are two different ways of creating the PKCS7. Once manually and once via a IExternalSignature(PrivateKeySignature) implementation. 

## Main application

For both cases, the pdf digest which must be signed is created in the same way. The only difference is the way the PKCS7 is created. 

The project on github (https://github.com/suntsu42/PdfSignSamplePkcs1) is complete and self contained. In the resources folder is a private key file (pfx) used for creating the signature as well as the root certificate. In order to run the example, it should be enough to just change the value of the **resourcePath** variable to accommodate your local system. 

```c#
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

```



## Manual creation of the pkcs7 signature

In this sample,  first create a PKCS1 signature using a local certificate. The created PKCS1 signature then is applied to the PdfPKCS7 container via **SetExternalDigest** 

The pdf created in this way is invalid. 

![InvalidPdf](\Resources\InvalidPdf.jpg)

``` c#
        public byte[] CreatePKCS7ViaPkcs1()
        {
            //Load the certificate used for signing
            signCertificatePrivateKey = LoadCertificateFromFile();

            // create sha256 message digest
            // This is from https://kb.itextpdf.com/home/it7kb/examples/how-to-use-a-digital-signing-service-dss-such-as-globalsign-with-itext-7
            // Not sure if this is required, but the created signature is invalid either way
            using (SHA256 sha256 = SHA256.Create())
            {
                Digest = sha256.ComputeHash(Digest);
            }

            //Create pkcs1 signature
            byte[] signature = null;
            using (var key = signCertificatePrivateKey.GetRSAPrivateKey())
            {
                signature = key.SignData(Digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            Org.BouncyCastle.X509.X509Certificate cert = DotNetUtilities.FromX509Certificate(signCertificatePrivateKey);
            PdfPKCS7 sgn = new PdfPKCS7(null, new[] { cert }, "SHA256", false);
            sgn.SetExternalDigest(signature, null, "RSA");

            //Return the complete PKCS7 CMS 
            return sgn.GetEncodedPKCS7(Digest, PdfSigner.CryptoStandard.CMS, null, null, null);
        }
```

## Create PKCS7 signature using PrivateKeySignature implementation

In this sample, the PKCS7 is created using iText PrivateKeySignature. The signature is created with the same digest and the same private key as in the other example. 

The pdf created here is valid. But since this approach doesn't allow the use of an external service for creating the signature, i cannot use it. 

![ValidPdf](\Resources\ValidPdf.jpg)

```c#
        public byte[] CreatePKCS7ViaPkcs1()
        {
            //Load the certificate used for signing
            signCertificatePrivateKey = LoadCertificateFromFile();

            // create sha256 message digest
            // This is from https://kb.itextpdf.com/home/it7kb/examples/how-to-use-a-digital-signing-service-dss-such-as-globalsign-with-itext-7
            // Not sure if this is required, but the created signature is invalid either way
            using (SHA256 sha256 = SHA256.Create())
            {
                Digest = sha256.ComputeHash(Digest);
            }

            //Create pkcs1 signature using RSA
            byte[] signature = null;
            using (var key = signCertificatePrivateKey.GetRSAPrivateKey())
            {
                signature = key.SignData(Digest, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            Org.BouncyCastle.X509.X509Certificate cert = DotNetUtilities.FromX509Certificate(signCertificatePrivateKey);
            PdfPKCS7 sgn = new PdfPKCS7(null, new[] { cert }, "SHA256", false);
            sgn.SetExternalDigest(signature, null, "RSA");

            //Return the complete PKCS7 CMS 
            return sgn.GetEncodedPKCS7(Digest, PdfSigner.CryptoStandard.CMS, null, null, null);
        }
```

