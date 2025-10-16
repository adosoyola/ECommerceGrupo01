USE [ECommerceDb];
GO

-- Permite la inserción de valores explícitos en la columna Id (IDENTITY)
SET IDENTITY_INSERT [dbo].[Products] ON;
GO

INSERT INTO [dbo].[Products] ([Id], [Name], [Price], [Stock], [Description], [ImagePath]) VALUES
(1, N'Notebook Lenovo', 3600.00, 15, N'LEGION 9 18IAX10, 18" WQUXGA IPS, Core Ultra 9 275HX 5.4GHz, 64GB', N'/images/nbg.jpg'),
(2, N'Unidad en estado solido', 250.00, 8, N'Kingston 1000GB NV3 PCIe 4.0 NVMe M.2 SSD', N'/images/ram.jpg'),
(3, N'Router D-Link', 180.00, 12, N'Smart AC1800 R18, 3 x LAN 10/100/1000Mbps, 1 x WAN 10/100/1000Mbps, 2.4/5GHz', N'/images/router.jpg'),
(4, N'SSD Kingston', 350.00, 15, N'Unidad en estado sólido externa Kingston XS1000, 1TB, USB 3.2 Gen 2 Tipo-C', N'/images/ssd.jpg'),
(5, N'Tablet Lenovo', 1200.00, 20, N'TB311FU, 10.1" WUXGA (1920x1200)/TFT/LCD/IPS/Touch/Android 14 o superior', N'/images/tablet.jpg'),
(6, N'Computadora Lenovo', 2200.00, 10, N'ThinkCentre M70q Gen 5 Core i5-14400T 1.5/4.5GHz, 16GB DDR5-4800 SODIMM', N'/images/pc.jpg'),
(7, N'Webcam TEROS', 160.00, 18, N'TE-9072, 2K, micrófono incorporado, USB 2.0', N'/images/webcam.jpg'),
(8, N'Tinta EPSON', 120.00, 21, N'T544120-AL, color Negro, contenido 65ml.', N'/images/tintanegra.jpg'),
(9, N'Impresora EPSON', 2000.00, 9, N'Multifuncional de tinta Epson EcoTank L3210, Imprime / Escanea / Copia / USB', N'/images/Impresora.jpg');
GO

-- Desactiva la inserción de valores explícitos en la columna Id
SET IDENTITY_INSERT [dbo].[Products] OFF;
GO

-- (Opcional) Verifica que los productos fueron insertados
SELECT * FROM [dbo].[Products];


