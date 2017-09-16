﻿using System;
using Microsoft.AspNetCore.Mvc;
using Impactro.Cobranca;
using System.DrawingCore;
using System.IO;

namespace BoletoASPnetCore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Boleto()
        {
            // Definição dos dados do cedente
            var Cedente = new CedenteInfo()
            {
                Cedente = "Teste de Cedente",
                CNPJ = "12.345.678/0000-12",
                Endereco = "endereço do recebedor",
                Banco = "237",
                Agencia = "0646",
                Conta = "9105-8",
                Carteira = "6"
            };

            // Definição dos dados do sacado
            var Sacado = new SacadoInfo()
            {
                Sacado = "Fabio Ferreira (Teste)",
                Documento = "123.456.789-99",
                Endereco = "Av. Paulista, 1234",
                Cidade = "São Paulo",
                Bairro = "Centro",
                Cep = "12345-123",
                UF = "SP"
            };

            // Definição das Variáveis do boleto
            var Boleto = new BoletoInfo()
            {
                NumeroDocumento = "00046356",
                NossoNumero = "00046356",
                ParcelaNumero = 1,
                ParcelaTotal = 3,
                Quantidade = 4,
                ValorUnitario = 34.55,
                ValorDocumento = 3070.14,
                DataDocumento = DateTime.Now,
                DataVencimento = DateTime.Parse("30/07/2017"),
                Demonstrativo = "Demostrativo para o cliente",
                Instrucoes = "Todas as informações deste bloqueto são de exclusiva responsabilidade do cedente"
            };

            // monta o boleto com os dados específicos nas classes
            var blt = new Boleto();
            blt.MakeBoleto(Cedente, Sacado, Boleto); // Armazena e inicializa variáveis internas
            blt.CalculaBoleto(); // Calcula a linha digitável e código de barras

            ViewBag.Boleto = blt; // Coloca na viewBag a instancia do boleto calculado para ser meschado no HTML

            return View();
        }

        // Copiado diretamente
        public static Bitmap BarCodeImage(string NumTexto, int nScale, float resolucao = 600f)
        {
            // Transforma o numero em uma string padrão de barras
            string cCodBar = CobUtil.BarCode(NumTexto);

            if (nScale < 3)
                throw new Exception("Escala minima é 3");

            // Ajusta a Escala padrão 
            //nScale /= 3; // Atenção, o ideal para a escala é ser multiplo de 3

            int wSF = nScale / 3;
            int wSL = nScale;

            // Codigo de Barras 2 por 5  =>  2 digitos são representados por 5 Barras PBPBP largas ou finas
            int nWidth = NumTexto.Length * 4 * nScale;

            Bitmap bmp = new Bitmap(nWidth, 50);
            bmp.SetResolution(resolucao, resolucao);
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.White);

            // Posição da linha atualmente desenhada (cursor)
            int nX = 0;
            for (int n = 0; n < cCodBar.Length; n += 2)
            {
                switch (cCodBar.Substring(n, 2))
                {
                    case "bf":
                        g.FillRectangle(Brushes.White, nX, 0, wSF, 50);
                        nX += wSF;
                        break;
                    case "pf":
                        g.FillRectangle(Brushes.Black, nX, 0, wSF, 50);
                        nX += wSF;
                        break;
                    case "bl":
                        g.FillRectangle(Brushes.White, nX, 0, wSL, 50);
                        nX += wSL;
                        break;
                    case "pl":
                        g.FillRectangle(Brushes.Black, nX, 0, wSL, 50);
                        nX += wSL;
                        break;
                }
            }

            // Extrai apenas a imagem 100% util
            Bitmap bmp2 = new Bitmap(nX, 50);
            bmp2.SetResolution(resolucao, resolucao);
            g = Graphics.FromImage(bmp2);
            g.DrawImage(bmp, 0, 0);

            return bmp2;
        }

        public ActionResult BarCodeImage(string id)
        {
            MemoryStream ms = new MemoryStream();
            var image = BarCodeImage(id, 3, 150);
            image.Save(ms, System.DrawingCore.Imaging.ImageFormat.Png);
            return File(ms.ToArray(), "image/png");
        }

        // Teste usando: ZKWeb.System.Drawing
        // https://stackoverflow.com/questions/186062/can-an-asp-net-mvc-controller-return-an-image
        // http://localhost:54796/home/GetModifiedImage
        public ActionResult GetModifiedImage()
        {
            var image = new Bitmap(200, 100);

            using (Graphics g = Graphics.FromImage(image))
            {
                // teste dos elementos basicos
                g.FillRectangle(Brushes.Yellow, 5, 5, 190, 50);
                g.DrawLine(Pens.Blue, 0, 0, 200, 100);
                g.DrawRectangle(Pens.Green, 5, 5, 180, 90);

                // do something with the Graphics (eg. write "Hello World!")
                string text = "Hello World!";

                // Create font and brush.
                Font drawFont = new Font("Arial", 10);
                SolidBrush drawBrush = new SolidBrush(Color.Red);

                // Create point for upper-left corner of drawing.
                PointF stringPoint = new PointF(10, 10);

                g.DrawString(text, drawFont, drawBrush, stringPoint);
            }

            MemoryStream ms = new MemoryStream();

            image.Save(ms, System.DrawingCore.Imaging.ImageFormat.Png);

            return File(ms.ToArray(), "image/png");
        }


        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
