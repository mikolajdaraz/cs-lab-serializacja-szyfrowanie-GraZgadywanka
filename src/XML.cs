using System;
using System.Xml;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Security.Cryptography.Xml;
using GraZaDuzoZaMalo.Model;

namespace AppGraZaDuzoZaMaloCLI
{
    public class SerializajcaXML : ZapiszGre
    {
        DataContractSerializer serializator = new DataContractSerializer(typeof(Gra));
        Aes klucz;

        public SerializajcaXML()
        {
            klucz = Aes.Create();
            klucz.GenerateKey();
            zapis = "zapis.xml";
        }

        public override Gra WczytajGre()
        {
            if (zapisDostepny)
            {
                Dekryptuj();
                Gra gra = null;
                using (var stream = new FileStream(zapis, FileMode.Open, FileAccess.Read))
                {
                    gra = Wczytywanie(stream);
                }
                Szyfruj();
                return gra;
            }
            else
            {
                throw new SerializationException();
            }
        }

        public override void SerializacjaGry(Gra gra)
        {
            if (zapisDostepny)
                Dekryptuj();

            using (var stream = new FileStream(zapis, FileMode.Create, FileAccess.Write))
            {
                Zapisywanie(stream, gra);
            }
            Szyfruj();
        }

        protected override void Zapisywanie(Stream stream, Gra gra)
        {
            serializator.WriteObject(stream, gra);
        }
        protected override Gra Wczytywanie(Stream stream)
        {
            return (Gra)serializator.ReadObject(stream);
        }

        ////////////////////////////////////////////////

        public void Szyfruj()
        {
            XmlDocument xml = new XmlDocument();
            xml.PreserveWhitespace = true;
            xml.Load(zapis);

            XmlElement plikDoZaszyfrowania = xml.GetElementsByTagName("liczbaDoOdgadniecia")[0] as XmlElement;
            EncryptedXml zaszyfrowanyXml = new EncryptedXml();
            byte[] zaszyfrowanyElement = zaszyfrowanyXml.EncryptData(plikDoZaszyfrowania, klucz, false);
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.Type = EncryptedXml.XmlEncElementUrl;
            string encryptionMethod = EncryptedXml.XmlEncAES256Url;
            encryptedData.EncryptionMethod = new EncryptionMethod(encryptionMethod);
            encryptedData.CipherData.CipherValue = zaszyfrowanyElement;
            EncryptedXml.ReplaceElement(plikDoZaszyfrowania, encryptedData, false);
            using (var stream = new FileStream(zapis, FileMode.Create, FileAccess.Write))
            {
                xml.Save(stream);
            }
        }

        public void Dekryptuj()
        {
            XmlDocument xml = new XmlDocument();
            xml.PreserveWhitespace = true;
            xml.Load(zapis);
            XmlElement zaszyfrowanyElement = xml.GetElementsByTagName("EncryptedData")[0] as XmlElement;
            if (zaszyfrowanyElement == null)
                throw new XmlException("Nie znaleziono pliku!");
            EncryptedData encryptedData = new EncryptedData();
            encryptedData.LoadXml(zaszyfrowanyElement);
            EncryptedXml zaszyfrowanyXml = new EncryptedXml();
            byte[] rgbOutput = zaszyfrowanyXml.DecryptData(encryptedData, klucz);
            zaszyfrowanyXml.ReplaceData(zaszyfrowanyElement, rgbOutput);
            using (var stream = new FileStream(zapis, FileMode.Create, FileAccess.Write))
            {
                xml.Save(stream);
            }
        }
    }

    public abstract class ZapiszGre
    {
        protected String zapis;
        virtual public void SerializacjaGry(Gra gra)
        {
            using (var stream = new FileStream(zapis, FileMode.Create, FileAccess.Write))
            {
                Zapisywanie(stream, gra);
            }
        }
        virtual public Gra WczytajGre()
        {
            using (var stream = new FileStream(zapis, FileMode.Open, FileAccess.Read))
            {
                return Wczytywanie(stream);
            }
        }
        virtual public void UsunZapis()
        {
            if (zapisDostepny)
            {
                File.Delete(zapis);
            }
        }
        public bool zapisDostepny { get => File.Exists(zapis); }
        protected abstract void Zapisywanie(Stream stream, Gra gra);
        protected abstract Gra Wczytywanie(Stream stream);
    }
}