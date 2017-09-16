using System;
using Microsoft.AspNetCore.Mvc;
using Impactro.Cobranca;
using System.DrawingCore;
using System.IO;
using System.Collections.Generic;

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

        private static object lockRender = new object();
        public ActionResult BarCodeImage(string id)
        {
            // quando manda renderizar muitas coisas a "ZKWeb.System.Drawing" gera alguma exception de recurso já em uso
            // então por hora, gero um lock enfilerando solicitações, que resolve, já que este método é bem rapido
            lock (lockRender)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    var image = BarCodeImage(id, 3, 150);
                    image.Save(ms, System.DrawingCore.Imaging.ImageFormat.Png);
                    return File(ms.ToArray(), "image/png");
                }
            }
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


        public IActionResult Error()
        {
            return View();
        }

        public IActionResult CaixaHomologa()
        {
            // Exemplo original em https://github.com/impactro/Boleto-ASP.NET/blob/master/BoletoNet/HomologaCaixaCS.aspx.cs adaptado minimamente para .net core
            // Aqui também aproveito para mostrar como colocar varios boletos na mesma página
            // Definição dos dados do cedente
            CedenteInfo Cedente = new CedenteInfo();
            Cedente.Cedente = "Exemplo de empresa cedente";
            Cedente.Endereco = "rua Qualquer no Bairro da Cidade";
            Cedente.CNPJ = "12.345.678/00001-12";
            Cedente.Banco = "104";
            Cedente.Agencia = "4353";
            Cedente.Conta = "00000939-9";
            Cedente.Carteira = "1"; // 1-Registrada ou 2-Sem registro
            Cedente.CodCedente = "658857";
            Cedente.Convenio = "1234"; // CNPJ do PV da conta do cliente = 00.360.305/4353-48 (usado em alguns casos)
            Cedente.Informacoes =
                "SAC CAIXA: 0800 726 0101 (informações, reclamações, sugestões e elogios)<br/>" +
                "Para pessoas com deficiência auditiva ou de fala: 0800 726 2492<br/>" +
                "Ouvidoria: 0800 725 7474 (reclamações não solucionadas e denúncias)<br/>" +
                "<a href='http://caixa.gov.br' target='_blank'>caixa.gov.br</a>";

            BoletoTextos.LocalPagamento = "PREFERENCIALMENTE NAS CASAS LOTÉRICAS ATÉ O VALOR LIMITE";

            // Definição dos dados do sacado
            SacadoInfo Sacado = new SacadoInfo();
            Sacado.Sacado = "Fabio Ferreira (Teste para homologação)";
            Sacado.Documento = "123.456.789-99";
            Sacado.Endereco = "Av. Paulista, 1234";
            Sacado.Cidade = "São Paulo";
            Sacado.Bairro = "Centro";
            Sacado.Cep = "12345-123";
            Sacado.UF = "SP";
            Sacado.Avalista = "CNPJ: 123.456.789/00001-23";

            // Para aprovar a homologação junto a caixa é necessário apresentar 10 boletos com os 10 digitos de controle da linha digitável diferentes
            // E mais outros 10 com o digito de controle do código de barras
            // Assim a ideia é criar 2 listas para ir memorizando os boletos já validos e deixa-los entrar em tela

            List<int> DAC1 = new List<int>();
            List<int> DAC2 = new List<int>();
            List<Boleto> boletoList = new List<Impactro.Cobranca.Boleto>();

            for (int nBoleto = 1001; nBoleto < 1100; nBoleto++)
            {
                // Definição dos dados do boleto de forma sequencial
                BoletoInfo Boleto = new BoletoInfo()
                {
                    NumeroDocumento = nBoleto.ToString(),
                    NossoNumero = nBoleto.ToString(),
                    ValorDocumento = 123.45,
                    DataVencimento = DateTime.Now,
                    DataDocumento = DateTime.Now,
                };

                // Componente HTML do boleto que poderá ser ou não colocado em tela
                var blt = new Boleto();
                blt.CalculaBoleto(); // Calcula a linha digitável e código de barras

                // Junta as informações para fazer o calculo
                blt.MakeBoleto(Cedente, Sacado, Boleto);

                // A instancia 'blt' é apenas un Webcontrol que renderiza o boleto HTML, tudo fica dentro da propriedade 'Boleto'
                blt.CalculaBoleto();
                // 10491.23456 60000.200042 00000.000844 4 67410000012345
                // 012345678901234567890123456789012345678901234567890123
                // 000000000111111111122222222223333333333444444444455555
                int D1 = int.Parse(blt.LinhaDigitavel.Substring(38, 1));
                int D2 = int.Parse(blt.LinhaDigitavel.Substring(35, 1));
                // De acordo com o banco:
                // Todos os Dígitos Verificadores Geral do Código de Barras possíveis(de 1 a 9) ou seja, campo 4 da Representação Numérica
                // Todas os Dígitos Verificadores do Campo Livre possíveis(de 0 a 9), 10ª posição   do campo 3 da Representação Numérica

                bool lUsar = false;
                if (!DAC1.Contains(D1))
                {
                    lUsar = true;
                    DAC1.Add(D1);
                }
                if (!DAC2.Contains(D2))
                {
                    lUsar = true;
                    DAC2.Add(D2);
                }

                if (lUsar)
                {
                    boletoList.Add(blt);
                }

                // Quando todas as possibilidades concluidas em até 100 boletos, já pode terminar...
                if (DAC1.Count == 9 && DAC2.Count == 10)
                    break; // o Modulo 11 padrão não tem o digito Zero, mas o especial para calculo do nosso numero tem
            }
            ViewBag.BoletoList = boletoList;
            return View();
        }
    }
}
