IF DB_ID(N'UP01_Andreev_423_FoodDelivery') IS NULL
    CREATE DATABASE [UP01_Andreev_423_FoodDelivery];
GO
USE [UP01_Andreev_423_FoodDelivery];
GO

IF OBJECT_ID(N'dbo.reviews', N'U') IS NOT NULL DROP TABLE dbo.[reviews];
IF OBJECT_ID(N'dbo.deliveries', N'U') IS NOT NULL DROP TABLE dbo.[deliveries];
IF OBJECT_ID(N'dbo.couriers', N'U') IS NOT NULL DROP TABLE dbo.[couriers];
IF OBJECT_ID(N'dbo.payments', N'U') IS NOT NULL DROP TABLE dbo.[payments];
IF OBJECT_ID(N'dbo.order_items', N'U') IS NOT NULL DROP TABLE dbo.[order_items];
IF OBJECT_ID(N'dbo.orders', N'U') IS NOT NULL DROP TABLE dbo.[orders];
IF OBJECT_ID(N'dbo.dishes', N'U') IS NOT NULL DROP TABLE dbo.[dishes];
IF OBJECT_ID(N'dbo.categories', N'U') IS NOT NULL DROP TABLE dbo.[categories];
IF OBJECT_ID(N'dbo.restaurants', N'U') IS NOT NULL DROP TABLE dbo.[restaurants];
IF OBJECT_ID(N'dbo.addresses', N'U') IS NOT NULL DROP TABLE dbo.[addresses];
IF OBJECT_ID(N'dbo.users', N'U') IS NOT NULL DROP TABLE dbo.[users];
IF OBJECT_ID(N'dbo.roles', N'U') IS NOT NULL DROP TABLE dbo.[roles];
GO

CREATE TABLE dbo.[roles]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_roles PRIMARY KEY,
    name NVARCHAR(50) NOT NULL CONSTRAINT UQ_roles_name UNIQUE
);
GO

CREATE TABLE dbo.[users]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_users PRIMARY KEY,
    role_id INT NOT NULL,
    login NVARCHAR(50) NOT NULL CONSTRAINT UQ_users_login UNIQUE,
    full_name NVARCHAR(150) NOT NULL,
    phone NVARCHAR(20) NOT NULL CONSTRAINT UQ_users_phone UNIQUE,
    email NVARCHAR(150) NULL,
    password_hash NVARCHAR(255) NOT NULL,
    created_at DATETIME2(0) NOT NULL CONSTRAINT DF_users_created_at DEFAULT SYSDATETIME(),
    is_active BIT NOT NULL CONSTRAINT DF_users_is_active DEFAULT (1),
    CONSTRAINT FK_users_roles FOREIGN KEY (role_id) REFERENCES dbo.roles(id),
    CONSTRAINT CK_users_phone CHECK (LEN(phone) BETWEEN 7 AND 20)
);
GO

CREATE TABLE dbo.[addresses]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_addresses PRIMARY KEY,
    user_id INT NOT NULL,
    address_text NVARCHAR(300) NOT NULL,
    latitude DECIMAL(9,6) NULL,
    longitude DECIMAL(9,6) NULL,
    CONSTRAINT FK_addresses_users FOREIGN KEY (user_id) REFERENCES dbo.users(id) ON DELETE CASCADE,
    CONSTRAINT CK_addresses_latitude CHECK (latitude IS NULL OR latitude BETWEEN -90 AND 90),
    CONSTRAINT CK_addresses_longitude CHECK (longitude IS NULL OR longitude BETWEEN -180 AND 180)
);
GO

CREATE TABLE dbo.[restaurants]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_restaurants PRIMARY KEY,
    name NVARCHAR(150) NOT NULL,
    address_text NVARCHAR(300) NOT NULL,
    phone NVARCHAR(20) NULL,
    description NVARCHAR(MAX) NULL,
    rating DECIMAL(2,1) NULL,
    is_active BIT NOT NULL CONSTRAINT DF_restaurants_is_active DEFAULT (1),
    CONSTRAINT CK_restaurants_rating CHECK (rating IS NULL OR rating BETWEEN 0 AND 5)
);
GO

CREATE TABLE dbo.[categories]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_categories PRIMARY KEY,
    restaurant_id INT NOT NULL,
    name NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_categories_restaurants FOREIGN KEY (restaurant_id) REFERENCES dbo.restaurants(id) ON DELETE CASCADE,
    CONSTRAINT UQ_categories_restaurant_name UNIQUE (restaurant_id, name)
);
GO

CREATE TABLE dbo.[dishes]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_dishes PRIMARY KEY,
    category_id INT NOT NULL,
    name NVARCHAR(150) NOT NULL,
    description NVARCHAR(MAX) NULL,
    price DECIMAL(10,2) NOT NULL,
    is_available BIT NOT NULL CONSTRAINT DF_dishes_is_available DEFAULT (1),
    CONSTRAINT FK_dishes_categories FOREIGN KEY (category_id) REFERENCES dbo.categories(id) ON DELETE CASCADE,
    CONSTRAINT CK_dishes_price CHECK (price >= 0)
);
GO

CREATE TABLE dbo.[orders]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_orders PRIMARY KEY,
    user_id INT NOT NULL,
    restaurant_id INT NOT NULL,
    address_id INT NOT NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_orders_status DEFAULT (N'Новый'),
    payment_method NVARCHAR(30) NOT NULL,
    total_amount DECIMAL(10,2) NOT NULL CONSTRAINT DF_orders_total_amount DEFAULT (0),
    created_at DATETIME2(0) NOT NULL CONSTRAINT DF_orders_created_at DEFAULT SYSDATETIME(),
    comment NVARCHAR(500) NULL,
    CONSTRAINT FK_orders_users FOREIGN KEY (user_id) REFERENCES dbo.users(id) ON DELETE CASCADE,
    CONSTRAINT FK_orders_restaurants FOREIGN KEY (restaurant_id) REFERENCES dbo.restaurants(id) ON DELETE CASCADE,
    CONSTRAINT FK_orders_addresses FOREIGN KEY (address_id) REFERENCES dbo.addresses(id),
    CONSTRAINT CK_orders_status CHECK (status IN (N'Новый',N'Принят',N'Готовится',N'Готов',N'Передан курьеру',N'Доставляется',N'Доставлен',N'Отменён')),
    CONSTRAINT CK_orders_payment_method CHECK (payment_method IN (N'Карта',N'Наличные',N'SberPay')),
    CONSTRAINT CK_orders_total CHECK (total_amount >= 0)
);
GO

CREATE TABLE dbo.[order_items]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_order_items PRIMARY KEY,
    order_id INT NOT NULL,
    dish_id INT NOT NULL,
    quantity INT NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    CONSTRAINT FK_order_items_orders FOREIGN KEY (order_id) REFERENCES dbo.orders(id) ON DELETE CASCADE,
    CONSTRAINT FK_order_items_dishes FOREIGN KEY (dish_id) REFERENCES dbo.dishes(id),
    CONSTRAINT CK_order_items_quantity CHECK (quantity > 0),
    CONSTRAINT CK_order_items_price CHECK (price >= 0),
    CONSTRAINT UQ_order_items_order_dish UNIQUE(order_id,dish_id)
);
GO

CREATE TABLE dbo.[payments]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_payments PRIMARY KEY,
    order_id INT NOT NULL CONSTRAINT UQ_payments_order UNIQUE,
    amount DECIMAL(10,2) NOT NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_payments_status DEFAULT (N'Ожидает'),
    paid_at DATETIME2(0) NULL,
    CONSTRAINT FK_payments_orders FOREIGN KEY (order_id) REFERENCES dbo.orders(id) ON DELETE CASCADE,
    CONSTRAINT CK_payments_amount CHECK (amount >= 0),
    CONSTRAINT CK_payments_status CHECK (status IN (N'Ожидает',N'Оплачено',N'Ошибка',N'Возврат'))
);
GO

CREATE TABLE dbo.[couriers]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_couriers PRIMARY KEY,
    user_id INT NOT NULL CONSTRAINT UQ_couriers_user UNIQUE,
    vehicle_type NVARCHAR(30) NULL,
    is_active BIT NOT NULL CONSTRAINT DF_couriers_is_active DEFAULT (1),
    CONSTRAINT FK_couriers_users FOREIGN KEY (user_id) REFERENCES dbo.users(id)
);
GO

CREATE TABLE dbo.[deliveries]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_deliveries PRIMARY KEY,
    order_id INT NOT NULL CONSTRAINT UQ_deliveries_order UNIQUE,
    courier_id INT NULL,
    status NVARCHAR(30) NOT NULL CONSTRAINT DF_deliveries_status DEFAULT (N'Ожидает курьера'),
    assigned_at DATETIME2(0) NULL,
    picked_up_at DATETIME2(0) NULL,
    delivered_at DATETIME2(0) NULL,
    CONSTRAINT FK_deliveries_orders FOREIGN KEY (order_id) REFERENCES dbo.orders(id) ON DELETE CASCADE,
    CONSTRAINT FK_deliveries_couriers FOREIGN KEY (courier_id) REFERENCES dbo.couriers(id) ON DELETE SET NULL,
    CONSTRAINT CK_deliveries_status CHECK (status IN (N'Ожидает курьера',N'Назначена',N'Забрана',N'В пути',N'Доставлена',N'Отменена'))
);
GO

CREATE TABLE dbo.[reviews]
(
    id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_reviews PRIMARY KEY,
    user_id INT NOT NULL,
    restaurant_id INT NOT NULL,
    order_id INT NOT NULL CONSTRAINT UQ_reviews_order UNIQUE,
    rating INT NOT NULL,
    comment NVARCHAR(1000) NULL,
    created_at DATETIME2(0) NOT NULL CONSTRAINT DF_reviews_created_at DEFAULT SYSDATETIME(),
    CONSTRAINT FK_reviews_users FOREIGN KEY (user_id) REFERENCES dbo.users(id),
    CONSTRAINT FK_reviews_restaurants FOREIGN KEY (restaurant_id) REFERENCES dbo.restaurants(id),
    CONSTRAINT FK_reviews_orders FOREIGN KEY (order_id) REFERENCES dbo.orders(id) ON DELETE CASCADE,
    CONSTRAINT CK_reviews_rating CHECK (rating BETWEEN 1 AND 5)
);
GO

-- Индексы для наиболее частых сценариев поиска и соединений.
CREATE INDEX IX_users_role_id ON dbo.users(role_id);
CREATE UNIQUE INDEX UQ_users_email ON dbo.users(email) WHERE email IS NOT NULL;
CREATE INDEX IX_addresses_user_id ON dbo.addresses(user_id);
CREATE INDEX IX_categories_restaurant_id ON dbo.categories(restaurant_id);
CREATE INDEX IX_dishes_category_id ON dbo.dishes(category_id);
CREATE INDEX IX_orders_user_id ON dbo.orders(user_id);
CREATE INDEX IX_orders_restaurant_id ON dbo.orders(restaurant_id);
CREATE INDEX IX_orders_address_id ON dbo.orders(address_id);
CREATE INDEX IX_orders_status_created_at ON dbo.orders(status, created_at);
CREATE INDEX IX_order_items_order_id ON dbo.order_items(order_id);
CREATE INDEX IX_deliveries_courier_id ON dbo.deliveries(courier_id);
CREATE INDEX IX_reviews_restaurant_id ON dbo.reviews(restaurant_id);
GO

-- Роли. Обычная регистрация в приложении создаёт только пользователя User.
INSERT INTO dbo.roles(name) VALUES (N'User'), (N'Admin'), (N'Restaurant'), (N'Courier');
GO

-- Демонстрационные учётные записи. Пароли хранятся только как salted PBKDF2-хеши.
INSERT INTO dbo.users(role_id, login, full_name, phone, email, password_hash, is_active)
VALUES
(2,N'admin',N'Администратор FoodGo',N'+70000000001',N'admin@foodgo.local',N'pbkdf2$100000$dwGSJF+YD88cApaEW9zMWA==$q4sLMM/JtSucagLeU+ozxXv/DwawxDHWzVBEKKon3bA=',1),
(1,N'timyan',N'Андреев Тимофей Андреевич',N'+70000000002',N'timyan@foodgo.local',N'pbkdf2$100000$qd3U8ySnTw1hURt8hi3pwA==$+iMFTzUT7AwSsiK6R/hCSqmJxVbXpWhErImF9iUDcfg=',1),
(1,N'ivan',N'Иванов Иван Иванович',N'+70000000003',N'ivan@foodgo.local',N'pbkdf2$100000$DGzr7laEazeMHZY0ehfnGg==$fe5Dh/itD7mYQfFwaJr7IfI9I0lLdch2DGyuI0Y9tNI=',1),
(1,N'anna',N'Петрова Анна Сергеевна',N'+70000000004',N'anna@foodgo.local',N'pbkdf2$100000$b7To5BBk8Is8AkTTFObzNg==$qFxxLOSUQE5tMXoubzBG2u0Q9k6mf/HlHegBy+IsKpc=',1),
(4,N'courier',N'Курьер FoodGo',N'+70000000005',N'courier@foodgo.local',N'pbkdf2$100000$9iy49aiCr7DwMicI/RP2Og==$/2YA+26K6zWaSle7xrTjy0Tt2InWE9fzTeEdyGHHquk=',1);
GO

INSERT INTO dbo.addresses(user_id, address_text, latitude, longitude)
VALUES
(2,N'Москва, ул. Профсоюзная, д. 20',55.653000,37.525000),
(2,N'Москва, ул. Профсоюзная, д. 20, офис 14',55.653100,37.525100),
(3,N'Москва, ул. Маросейка, д. 12',55.757000,37.634000),
(4,N'Москва, ул. Таганская, д. 7',55.740000,37.653000);
GO

INSERT INTO dbo.restaurants(name,address_text,phone,description,rating,is_active)
VALUES
(N'Сытый лис',N'Москва, ул. Тверская, д. 10',N'+7 495 100-10-10',N'Европейская кухня и домашние блюда.',4.7,1),
(N'Восточный дом',N'Москва, ул. Мясницкая, д. 24',N'+7 495 200-20-20',N'Азиатская кухня, лапша и рис.',4.5,1),
(N'Пицца Дом',N'Москва, ул. Арбат, д. 15',N'+7 495 300-30-30',N'Пицца и итальянская кухня.',4.3,1);
GO

INSERT INTO dbo.categories(restaurant_id,name)
VALUES (1,N'Основные блюда'),(1,N'Напитки'),(2,N'Роллы'),(2,N'Горячее'),(3,N'Пицца'),(3,N'Закуски');
GO

INSERT INTO dbo.dishes(category_id,name,description,price,is_available)
VALUES
(1,N'Курица с рисом',N'Куриное филе, рис и овощи.',420.00,1),
(1,N'Паста с грибами',N'Паста в сливочном соусе.',510.00,1),
(2,N'Морс ягодный',N'Домашний ягодный морс 0,5 л.',150.00,1),
(3,N'Ролл Филадельфия',N'Лосось, сыр и огурец.',590.00,1),
(4,N'Лапша с курицей',N'Лапша с курицей и овощами.',470.00,1),
(5,N'Маргарита',N'Томатный соус, моцарелла, базилик.',650.00,1),
(5,N'Пепперони',N'Пепперони и моцарелла.',790.00,1),
(6,N'Картофель по-деревенски',N'Запечённый картофель со специями.',260.00,1);
GO

INSERT INTO dbo.couriers(user_id,vehicle_type,is_active) SELECT id,N'Велосипед',1 FROM dbo.users WHERE login=N'courier';
GO

INSERT INTO dbo.orders(user_id,restaurant_id,address_id,status,payment_method,total_amount,created_at,comment)
VALUES
(2,1,1,N'Доставлен',N'Карта',720.00,'2026-10-10T12:10:00',N'Позвонить перед доставкой'),
(2,3,2,N'Доставлен',N'SberPay',1440.00,'2026-10-11T18:45:00',NULL),
(3,2,3,N'Готовится',N'Наличные',1060.00,'2026-10-13T15:30:00',N'Без лука'),
(4,1,4,N'Новый',N'Карта',570.00,'2026-10-14T12:40:00',NULL);
GO

INSERT INTO dbo.order_items(order_id,dish_id,quantity,price)
VALUES (1,1,1,420.00),(1,3,2,150.00),(2,6,1,650.00),(2,7,1,790.00),(3,4,1,590.00),(3,5,1,470.00),(4,1,1,420.00),(4,3,1,150.00);
GO

INSERT INTO dbo.payments(order_id,amount,status,paid_at)
VALUES (1,720.00,N'Оплачено','2026-10-10T12:11:00'),(2,1440.00,N'Оплачено','2026-10-11T18:46:00'),(3,1060.00,N'Ожидает',NULL),(4,570.00,N'Ожидает',NULL);
GO

INSERT INTO dbo.deliveries(order_id,courier_id,status,assigned_at,picked_up_at,delivered_at)
VALUES (1,1,N'Доставлена','2026-10-10T12:30:00','2026-10-10T12:40:00','2026-10-10T13:00:00'),
       (2,1,N'Доставлена','2026-10-11T19:00:00','2026-10-11T19:10:00','2026-10-11T19:45:00'),
       (3,NULL,N'Ожидает курьера',NULL,NULL,NULL),
       (4,NULL,N'Ожидает курьера',NULL,NULL,NULL);
GO

INSERT INTO dbo.reviews(user_id,restaurant_id,order_id,rating,comment,created_at)
VALUES (2,1,1,5,N'Вкусно, доставка вовремя.','2026-10-10T13:10:00'),
       (2,3,2,4,N'Пицца хорошая.','2026-10-11T20:00:00');
GO

-- Контрольная выборка: все двенадцать таблиц должны возвращать строки.
SELECT 'roles' AS table_name, COUNT(*) AS rows_count FROM dbo.roles UNION ALL
SELECT 'users', COUNT(*) FROM dbo.users UNION ALL
SELECT 'addresses', COUNT(*) FROM dbo.addresses UNION ALL
SELECT 'restaurants', COUNT(*) FROM dbo.restaurants UNION ALL
SELECT 'categories', COUNT(*) FROM dbo.categories UNION ALL
SELECT 'dishes', COUNT(*) FROM dbo.dishes UNION ALL
SELECT 'orders', COUNT(*) FROM dbo.orders UNION ALL
SELECT 'order_items', COUNT(*) FROM dbo.order_items UNION ALL
SELECT 'payments', COUNT(*) FROM dbo.payments UNION ALL
SELECT 'couriers', COUNT(*) FROM dbo.couriers UNION ALL
SELECT 'deliveries', COUNT(*) FROM dbo.deliveries UNION ALL
SELECT 'reviews', COUNT(*) FROM dbo.reviews;
GO
