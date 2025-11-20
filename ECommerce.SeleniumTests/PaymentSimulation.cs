using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Linq; // ✅ NECESARIO para usar .Last()

namespace ECommerce.SeleniumTests
{
    public class PurchaseFlowTests
    {
        private IWebDriver _driver;
        // ⚠️ Asegúrate de que este puerto coincida con el de tu proyecto .NET corriendo
        private const string BaseUrl = "http://localhost:5012";

        // Datos de prueba
        private const string CustomerUser = "prueba@gmail.com";
        private const string CustomerPass = "Prueba123@";

        [SetUp]
        public void Setup()
        {
            _driver = new ChromeDriver();
            _driver.Manage().Window.Maximize();
            // Damos un tiempo de espera implícito para que Selenium encuentre los elementos
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        }

        [Test]
        public void FlujoDeCompra_Completo_ClienteLogueado()
        {
            // ==========================================
            // PASO 1: LOGIN
            // ==========================================
            _driver.Navigate().GoToUrl($"{BaseUrl}/Identity/Account/Login");

            _driver.FindElement(By.Name("Input.Email")).SendKeys(CustomerUser);
            _driver.FindElement(By.Id("passwordInput")).SendKeys(CustomerPass);

            // Selector específico para el botón de login
            var loginBtn = _driver.FindElement(By.CssSelector("form#account button[type='submit']"));
            loginBtn.Click();

            Assert.That(_driver.Url, Does.Not.Contain("/Account/Login"), "El login falló, seguimos en la URL de login.");


            // ==========================================
            // PASO 2: IR AL DETALLE DEL PRODUCTO Y AGREGAR
            // ==========================================
            _driver.Navigate().GoToUrl($"{BaseUrl}/Products/Details/1");

            // Buscamos el botón dentro del formulario de agregar al carrito
            var addToCartBtn = _driver.FindElement(By.CssSelector("form[action*='/Cart/Add'] button[type='submit']"));
            addToCartBtn.Click();


            // ==========================================
            // PASO 3: VERIFICAR CARRITO E IR A CHECKOUT
            // ==========================================
            Assert.That(_driver.Url, Does.Contain("/Cart"), "No fuimos redirigidos al carrito.");

            // Botón para ir a confirmar compra
            var goToConfirmBtn = _driver.FindElement(By.CssSelector("form[action*='/Checkout/Confirm'] button[type='submit']"));
            goToConfirmBtn.Click();


            // ==========================================
            // PASO 4: CONFIRMAR COMPRA (CORREGIDO)
            // ==========================================
            // AQUI FALLABA: El selector genérico clickeaba "Logout" en el navbar.
            // Solución: Buscamos específicamente el formulario de PaymentSimulation O el último botón de la página.

            try
            {
                // Intento 1: Buscar formulario específico que va a la simulación
                var simulBtn = _driver.FindElement(By.CssSelector("form[action*='PaymentSimulation'] button[type='submit']"));
                simulBtn.Click();
            }
            catch (NoSuchElementException)
            {
                // Intento 2: Si no hay form explícito, buscamos botones dentro de <main> (evita el navbar)
                var mainButtons = _driver.FindElements(By.CssSelector("main button[type='submit']"));

                if (mainButtons.Count > 0)
                {
                    // Clic en el último botón encontrado en el main (suele ser "Confirmar" o "Siguiente")
                    mainButtons.Last().Click();
                }
                else
                {
                    // Fallback: Buscar TODOS los botones submit de la página y hacer clic en el ÚLTIMO
                    // (El logout suele ser el primero, el de confirmar suele estar al final)
                    var allSubmitButtons = _driver.FindElements(By.CssSelector("button[type='submit']"));
                    if (allSubmitButtons.Count > 0)
                    {
                        allSubmitButtons.Last().Click();
                    }
                    else
                    {
                        throw new Exception("No se encontró ningún botón para confirmar la compra en el paso 4.");
                    }
                }
            }


            // ==========================================
            // PASO 5: LLENAR DATOS DE PAGO (Simulación)
            // ==========================================
            // Aumentamos el tiempo de espera a 10 segundos
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));

            try
            {
                wait.Until(d => d.Url.Contains("/PaymentSimulation"));
            }
            catch (WebDriverTimeoutException)
            {
                // Si falla, imprimimos la URL actual para saber dónde se quedó
                throw new Exception($"Timeout esperando /PaymentSimulation. La URL actual es: {_driver.Url}");
            }

            _driver.FindElement(By.Id("CardNumber")).Clear();
            _driver.FindElement(By.Id("CardNumber")).SendKeys("4111111111111111");

            _driver.FindElement(By.Id("Expiration")).Clear();
            _driver.FindElement(By.Id("Expiration")).SendKeys("12/28");

            _driver.FindElement(By.Id("CVV")).Clear();
            _driver.FindElement(By.Id("CVV")).SendKeys("123");


            // ==========================================
            // PASO 6: ENVIAR PAGO FINAL
            // ==========================================
            // Usamos un selector seguro buscando el form de proceso
            try
            {
                var payBtn = _driver.FindElement(By.CssSelector("form[action*='ProcessPaymentSimulation'] button[type='submit']"));
                payBtn.Click();
            }
            catch (NoSuchElementException)
            {
                // Fallback: último botón del main
                _driver.FindElements(By.CssSelector("main button[type='submit']")).Last().Click();
            }


            // ==========================================
            // PASO 7: VERIFICAR ÉXITO
            // ==========================================
            wait.Until(d => d.Url.Contains("/Checkout/Success"));

            Assert.That(_driver.Url, Does.Contain("/Checkout/Success"), "La URL final no es la de éxito.");

            // Verificación de texto opcional
            var bodyText = _driver.FindElement(By.TagName("body")).Text;
            Assert.That(bodyText, Does.Contain("Exitosa").Or.Contain("Success").Or.Contain("Gracias"),
                "No se encontró mensaje de éxito en el cuerpo de la página.");
        }

        [TearDown]
        public void Teardown()
        {
            if (_driver != null)
            {
                _driver.Quit();
                _driver.Dispose();
            }
        }
    }
}