


INSERT INTO Products (Name, Price, Stock, Description,ImagePath)
VALUES ('Notebook Lenovo', 3600,15,'LEGION 9 18IAX10, 18" WQUXGA IPS, Core Ultra 9 275HX 5.4GHz, 64GB', '/images/nbg.jpg');
INSERT INTO Products (Name, Price, Stock, Description,ImagePath)
VALUES ('Unidad en estado solido', 250,8,'Kingston 1000GB NV3 PCIe 4.0 NVMe M.2 SSD', '/images/ram.jpg');
INSERT INTO Products (Name, Price, Stock, Description,ImagePath)
VALUES ('Router D-Link', 180,12,'Smart AC1800 R18, 3 x LAN 10/100/1000Mbps, 1 x WAN 10/100/1000Mbps, 2.4/5GHz', '/images/router.jpg');
INSERT INTO Products (Name, Price, Stock, Description,ImagePath)
VALUES ('SSD Kingston', 350,15,'Unidad en estado sólido externa Kingston XS1000, 1TB, USB 3.2 Gen 2 Tipo-C', '/images/ssd.jpg');
INSERT INTO Products (Name, Price, Stock, Description,ImagePath)
VALUES ('Tablet Lenovo', 1200,20,'TB311FU, 10.1" WUXGA (1920x1200)/TFT/LCD/IPS/Touch/Android 14 o superior', '/images/tablet.jpg');
INSERT INTO Products (Name, Price, Stock, Description,ImagePath)
VALUES ('Computadora Lenovo', 2200,10,'ThinkCentre M70q Gen 5 Core i5-14400T 1.5/4.5GHz, 16GB DDR5-4800 SODIMM', '/images/pc.jpg');
INSERT INTO Products (Name, Price, Stock, Description,ImagePath)
VALUES ('Webcam TEROS', 160,18,'TE-9072, 2K, micrófono incorporado, USB 2.0', '/images/webcam.jpg');
INSERT INTO Products (Name, Price, Stock, Description,ImagePath)
VALUES ('Tinta EPSON', 120,21,'T544120-AL, color Negro, contenido 65ml.', '/images/tintanegra.jpg');
INSERT INTO Products (Name, Price, Stock, Description,ImagePath)
VALUES ('Impresora EPSON', 2000,9,'Multifuncional de tinta Epson EcoTank L3210, Imprime / Escanea / Copia / USB', '/images/Impresora.jpg');

select * from Products

select * from Orders

DELETE FROM Products
WHERE Id BETWEEN 20 AND 28;

