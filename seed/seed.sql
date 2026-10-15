-- =============================================================================
-- COMPRAI — Seed Data
-- Uso: psql -h <host> -U <user> -d <db> -f seed.sql
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- Customers
-- -----------------------------------------------------------------------------
INSERT INTO customer (id, name, email, phone, role, email_verified, totp_enabled, created_at) VALUES
  ('a1000000-0000-0000-0000-000000000001', 'Ana Lima',        'ana.lima@email.com',       '11987650001', 'customer', true,  false, NOW() - INTERVAL '45 days'),
  ('a1000000-0000-0000-0000-000000000002', 'Bruno Souza',     'bruno.souza@email.com',    '21987650002', 'customer', true,  true,  NOW() - INTERVAL '40 days'),
  ('a1000000-0000-0000-0000-000000000003', 'Carla Mendes',    'carla.mendes@email.com',   '31987650003', 'customer', true,  false, NOW() - INTERVAL '38 days'),
  ('a1000000-0000-0000-0000-000000000004', 'Diego Ferreira',  'diego.ferreira@email.com', '41987650004', 'customer', false, false, NOW() - INTERVAL '35 days'),
  ('a1000000-0000-0000-0000-000000000005', 'Elisa Costa',     'elisa.costa@email.com',    '51987650005', 'customer', true,  true,  NOW() - INTERVAL '30 days'),
  ('a1000000-0000-0000-0000-000000000006', 'Fabio Rocha',     'fabio.rocha@email.com',    '61987650006', 'customer', true,  false, NOW() - INTERVAL '28 days'),
  ('a1000000-0000-0000-0000-000000000007', 'Gabriela Nunes',  'gabi.nunes@email.com',     '71987650007', 'customer', true,  true,  NOW() - INTERVAL '25 days'),
  ('a1000000-0000-0000-0000-000000000008', 'Henrique Alves',  'henrique.alves@email.com', '81987650008', 'customer', false, false, NOW() - INTERVAL '22 days'),
  ('a1000000-0000-0000-0000-000000000009', 'Isabela Pinto',   'isa.pinto@email.com',      '91987650009', 'customer', true,  false, NOW() - INTERVAL '20 days'),
  ('a1000000-0000-0000-0000-000000000010', 'Joao Martins',    'joao.martins@email.com',   '11987650010', 'customer', true,  true,  NOW() - INTERVAL '18 days'),
  ('a1000000-0000-0000-0000-000000000011', 'Karen Oliveira',  'karen.oliveira@email.com', '21987650011', 'customer', true,  false, NOW() - INTERVAL '15 days'),
  ('a1000000-0000-0000-0000-000000000012', 'Lucas Barbosa',   'lucas.barbosa@email.com',  '31987650012', 'customer', true,  false, NOW() - INTERVAL '13 days'),
  ('a1000000-0000-0000-0000-000000000013', 'Mariana Torres',  'mari.torres@email.com',    '41987650013', 'customer', true,  true,  NOW() - INTERVAL '11 days'),
  ('a1000000-0000-0000-0000-000000000014', 'Nicolas Pereira', 'nicolas.p@email.com',      '51987650014', 'customer', false, false, NOW() - INTERVAL '9 days'),
  ('a1000000-0000-0000-0000-000000000015', 'Olga Ramos',      'olga.ramos@email.com',     '61987650015', 'customer', true,  false, NOW() - INTERVAL '7 days'),
  ('a1000000-0000-0000-0000-000000000016', 'Pedro Carvalho',  'pedro.carvalho@email.com', '71987650016', 'customer', true,  true,  NOW() - INTERVAL '6 days'),
  ('a1000000-0000-0000-0000-000000000017', 'Queila Santos',   'queila.s@email.com',       '81987650017', 'customer', true,  false, NOW() - INTERVAL '5 days'),
  ('a1000000-0000-0000-0000-000000000018', 'Rafael Gomes',    'rafael.gomes@email.com',   '91987650018', 'customer', true,  false, NOW() - INTERVAL '4 days'),
  ('a1000000-0000-0000-0000-000000000019', 'Sara Freitas',    'sara.freitas@email.com',   '11987650019', 'customer', true,  true,  NOW() - INTERVAL '3 days'),
  ('a1000000-0000-0000-0000-000000000020', 'Thiago Melo',     'thiago.melo@email.com',    '21987650020', 'customer', true,  false, NOW() - INTERVAL '1 day')
ON CONFLICT (id) DO NOTHING;

INSERT INTO "order" (id, idempotency_key, customer_id, status, total_amount, tracking_code, shipping_zip, shipping_street, shipping_number, shipping_city, shipping_state, created_at) VALUES
  ('b1000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000001', 'delivered', 349.90, 'BR123456781BR', '01310-100', 'Av Paulista',     '1000', 'Sao Paulo',      'SP', NOW() - INTERVAL '29 days'),
  ('b1000000-0000-0000-0000-000000000002', 'c1000000-0000-0000-0000-000000000002', 'a1000000-0000-0000-0000-000000000002', 'delivered', 129.90, 'BR123456782BR', '20040-020', 'Rua da Quitanda', '86',   'Rio de Janeiro', 'RJ', NOW() - INTERVAL '27 days'),
  ('b1000000-0000-0000-0000-000000000003', 'c1000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000003', 'delivered', 599.00, 'BR123456783BR', '30112-010', 'Av Afonso Pena',  '2000', 'Belo Horizonte', 'MG', NOW() - INTERVAL '25 days'),
  ('b1000000-0000-0000-0000-000000000004', 'c1000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000004', 'delivered',  89.90, 'BR123456784BR', '80010-010', 'Rua XV Nov.',     '300',  'Curitiba',       'PR', NOW() - INTERVAL '23 days'),
  ('b1000000-0000-0000-0000-000000000005', 'c1000000-0000-0000-0000-000000000005', 'a1000000-0000-0000-0000-000000000005', 'delivered', 215.50, 'BR123456785BR', '90010-150', 'Av Borges Medeiros','500', 'Porto Alegre',   'RS', NOW() - INTERVAL '21 days'),
  ('b1000000-0000-0000-0000-000000000006', 'c1000000-0000-0000-0000-000000000006', 'a1000000-0000-0000-0000-000000000006', 'delivered', 445.00, 'BR123456786BR', '40010-010', 'Av Sete Set.',    '1500', 'Salvador',       'BA', NOW() - INTERVAL '19 days'),
  ('b1000000-0000-0000-0000-000000000007', 'c1000000-0000-0000-0000-000000000007', 'a1000000-0000-0000-0000-000000000007', 'delivered', 310.00, 'BR123456787BR', '60160-195', 'Av Beira Mar',    '200',  'Fortaleza',      'CE', NOW() - INTERVAL '17 days'),
  ('b1000000-0000-0000-0000-000000000008', 'c1000000-0000-0000-0000-000000000008', 'a1000000-0000-0000-0000-000000000001', 'delivered', 178.00, 'BR123456788BR', '01310-100', 'Av Paulista',     '1000', 'Sao Paulo',      'SP', NOW() - INTERVAL '15 days'),
  ('b1000000-0000-0000-0000-000000000009', 'c1000000-0000-0000-0000-000000000009', 'a1000000-0000-0000-0000-000000000009', 'delivered', 520.00, 'BR123456789BR', '69010-060', 'Av Eduardo Ribeiro','600','Manaus',          'AM', NOW() - INTERVAL '13 days'),
  ('b1000000-0000-0000-0000-000000000010', 'c1000000-0000-0000-0000-000000000010', 'a1000000-0000-0000-0000-000000000010', 'delivered',  99.90, 'BR123456790BR', '79002-010', 'Rua 13 de Maio',  '1150', 'Campo Grande',   'MS', NOW() - INTERVAL '11 days'),
  ('b1000000-0000-0000-0000-000000000011', 'c1000000-0000-0000-0000-000000000011', 'a1000000-0000-0000-0000-000000000011', 'shipped',   289.00, 'BR223456791BR', '01310-100', 'Rua Augusta',     '400',  'Sao Paulo',      'SP', NOW() - INTERVAL '6 days'),
  ('b1000000-0000-0000-0000-000000000012', 'c1000000-0000-0000-0000-000000000012', 'a1000000-0000-0000-0000-000000000012', 'shipped',   159.90, 'BR223456792BR', '20040-020', 'Rua Uruguaiana',  '50',   'Rio de Janeiro', 'RJ', NOW() - INTERVAL '5 days'),
  ('b1000000-0000-0000-0000-000000000013', 'c1000000-0000-0000-0000-000000000013', 'a1000000-0000-0000-0000-000000000013', 'shipped',   670.00, 'BR223456793BR', '30130-100', 'Rua dos Caetes',  '321',  'Belo Horizonte', 'MG', NOW() - INTERVAL '4 days'),
  ('b1000000-0000-0000-0000-000000000014', 'c1000000-0000-0000-0000-000000000014', 'a1000000-0000-0000-0000-000000000014', 'processing',199.90, NULL,            '80010-010', 'Rua Marechal Deodoro','100','Curitiba',      'PR', NOW() - INTERVAL '3 days'),
  ('b1000000-0000-0000-0000-000000000015', 'c1000000-0000-0000-0000-000000000015', 'a1000000-0000-0000-0000-000000000015', 'processing',349.00, NULL,            '90010-150', 'Rua dos Andradas', '700', 'Porto Alegre',   'RS', NOW() - INTERVAL '2 days'),
  ('b1000000-0000-0000-0000-000000000016', 'c1000000-0000-0000-0000-000000000016', 'a1000000-0000-0000-0000-000000000016', 'processing',499.00, NULL,            '40010-010', 'Rua Chile',       '200',  'Salvador',       'BA', NOW() - INTERVAL '2 days'),
  ('b1000000-0000-0000-0000-000000000017', 'c1000000-0000-0000-0000-000000000017', 'a1000000-0000-0000-0000-000000000017', 'confirmed', 125.00, NULL,            '60160-195', 'Rua Major Facundo','45',  'Fortaleza',      'CE', NOW() - INTERVAL '1 day'),
  ('b1000000-0000-0000-0000-000000000018', 'c1000000-0000-0000-0000-000000000018', 'a1000000-0000-0000-0000-000000000018', 'confirmed', 239.90, NULL,            '01310-100', 'Rua Haddock Lobo','350',  'Sao Paulo',      'SP', NOW() - INTERVAL '1 day'),
  ('b1000000-0000-0000-0000-000000000019', 'c1000000-0000-0000-0000-000000000019', 'a1000000-0000-0000-0000-000000000019', 'pending',    89.90, NULL,            '20040-020', 'Rua do Ouvidor',  '20',   'Rio de Janeiro', 'RJ', NOW() - INTERVAL '12 hours'),
  ('b1000000-0000-0000-0000-000000000020', 'c1000000-0000-0000-0000-000000000020', 'a1000000-0000-0000-0000-000000000020', 'pending',   415.00, NULL,            '30112-010', 'Av do Contorno',  '500',  'Belo Horizonte', 'MG', NOW() - INTERVAL '6 hours'),
  ('b1000000-0000-0000-0000-000000000021', 'c1000000-0000-0000-0000-000000000021', 'a1000000-0000-0000-0000-000000000003', 'cancelled', 199.90, NULL,            '80010-010', 'Rua XV Nov.',     '300',  'Curitiba',       'PR', NOW() - INTERVAL '20 days'),
  ('b1000000-0000-0000-0000-000000000022', 'c1000000-0000-0000-0000-000000000022', 'a1000000-0000-0000-0000-000000000007', 'cancelled',  89.90, NULL,            '90010-150', 'Av Independencia','800',  'Porto Alegre',   'RS', NOW() - INTERVAL '14 days'),
  ('b1000000-0000-0000-0000-000000000023', 'c1000000-0000-0000-0000-000000000023', 'a1000000-0000-0000-0000-000000000012', 'cancelled', 320.00, NULL,            '69010-060', 'Av Djalma Batista','1200','Manaus',          'AM', NOW() - INTERVAL '8 days')
ON CONFLICT (id) DO NOTHING;

INSERT INTO order_item (id, order_id, product_id, product_title, sku, quantity, unit_price, image_url) VALUES
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000001', 'PROD-001', 'Tenis Nike Air Max 90',          'NK-AIR-42', 1, 349.90, 'https://picsum.photos/seed/001/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000002', 'PROD-002', 'Camiseta Adidas Original',       'AD-CAM-M',  1, 129.90, 'https://picsum.photos/seed/002/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000003', 'PROD-003', 'Smartwatch Samsung Galaxy Watch','SAM-GW6',   1, 599.00, 'https://picsum.photos/seed/003/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000004', 'PROD-004', 'Livro Clean Code',               'BK-CC-PT',  1,  89.90, 'https://picsum.photos/seed/004/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000005', 'PROD-001', 'Tenis Nike Air Max 90',          'NK-AIR-40', 1, 215.50, 'https://picsum.photos/seed/001/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000006', 'PROD-005', 'Fone JBL Tune 510BT',            'JBL-510',   1, 245.00, 'https://picsum.photos/seed/005/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000006', 'PROD-006', 'Cabo USB-C 2m',                  'CBL-USC2',  2, 100.00, 'https://picsum.photos/seed/006/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000007', 'PROD-003', 'Smartwatch Samsung Galaxy Watch','SAM-GW6',   1, 310.00, 'https://picsum.photos/seed/003/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000008', 'PROD-007', 'Mouse Logitech MX Master 3',     'LOG-MX3',   1, 178.00, 'https://picsum.photos/seed/007/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000009', 'PROD-008', 'Teclado Mecanico Keychron K2',   'KEY-K2',    1, 520.00, 'https://picsum.photos/seed/008/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000010', 'PROD-004', 'Livro Clean Code',               'BK-CC-PT',  1,  99.90, 'https://picsum.photos/seed/004/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000011', 'PROD-005', 'Fone JBL Tune 510BT',            'JBL-510',   1, 289.00, 'https://picsum.photos/seed/005/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000012', 'PROD-009', 'Mousepad XL Gamer',              'MP-XL-BK',  1, 159.90, 'https://picsum.photos/seed/009/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000013', 'PROD-003', 'Smartwatch Samsung Galaxy Watch','SAM-GW6',   1, 599.00, 'https://picsum.photos/seed/003/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000013', 'PROD-006', 'Cabo USB-C 2m',                  'CBL-USC2',  1,  71.00, 'https://picsum.photos/seed/006/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000014', 'PROD-007', 'Mouse Logitech MX Master 3',     'LOG-MX3',   1, 199.90, 'https://picsum.photos/seed/007/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000015', 'PROD-001', 'Tenis Nike Air Max 90',          'NK-AIR-41', 1, 349.00, 'https://picsum.photos/seed/001/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000016', 'PROD-008', 'Teclado Mecanico Keychron K2',   'KEY-K2',    1, 499.00, 'https://picsum.photos/seed/008/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000017', 'PROD-004', 'Livro Clean Code',               'BK-CC-PT',  1,  89.90, 'https://picsum.photos/seed/004/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000017', 'PROD-006', 'Cabo USB-C 2m',                  'CBL-USC2',  1,  35.10, 'https://picsum.photos/seed/006/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000018', 'PROD-009', 'Mousepad XL Gamer',              'MP-XL-BK',  1, 239.90, 'https://picsum.photos/seed/009/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000019', 'PROD-002', 'Camiseta Adidas Original',       'AD-CAM-G',  1,  89.90, 'https://picsum.photos/seed/002/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000020', 'PROD-005', 'Fone JBL Tune 510BT',            'JBL-510',   1, 245.00, 'https://picsum.photos/seed/005/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000020', 'PROD-007', 'Mouse Logitech MX Master 3',     'LOG-MX3',   1, 170.00, 'https://picsum.photos/seed/007/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000021', 'PROD-002', 'Camiseta Adidas Original',       'AD-CAM-M',  1, 129.90, 'https://picsum.photos/seed/002/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000021', 'PROD-006', 'Cabo USB-C 2m',                  'CBL-USC2',  1,  70.00, 'https://picsum.photos/seed/006/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000022', 'PROD-004', 'Livro Clean Code',               'BK-CC-PT',  1,  89.90, 'https://picsum.photos/seed/004/80/80'),
  (gen_random_uuid(), 'b1000000-0000-0000-0000-000000000023', 'PROD-008', 'Teclado Mecanico Keychron K2',   'KEY-K2',    1, 320.00, 'https://picsum.photos/seed/008/80/80')
ON CONFLICT DO NOTHING;

INSERT INTO payment (id, idempotency_key, order_id, provider, method, status, amount) VALUES
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000001', 'b1000000-0000-0000-0000-000000000001', 'mock', 'pix',         'confirmed', 349.90),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000002', 'b1000000-0000-0000-0000-000000000002', 'mock', 'credit_card', 'confirmed', 129.90),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000003', 'b1000000-0000-0000-0000-000000000003', 'mock', 'pix',         'confirmed', 599.00),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000004', 'b1000000-0000-0000-0000-000000000004', 'mock', 'debit_card',  'confirmed',  89.90),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000005', 'b1000000-0000-0000-0000-000000000005', 'mock', 'credit_card', 'confirmed', 215.50),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000006', 'b1000000-0000-0000-0000-000000000006', 'mock', 'pix',         'confirmed', 445.00),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000007', 'b1000000-0000-0000-0000-000000000007', 'mock', 'credit_card', 'confirmed', 310.00),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000008', 'b1000000-0000-0000-0000-000000000008', 'mock', 'pix',         'confirmed', 178.00),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000009', 'b1000000-0000-0000-0000-000000000009', 'mock', 'credit_card', 'confirmed', 520.00),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000010', 'b1000000-0000-0000-0000-000000000010', 'mock', 'debit_card',  'confirmed',  99.90),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000011', 'b1000000-0000-0000-0000-000000000011', 'mock', 'pix',         'confirmed', 289.00),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000012', 'b1000000-0000-0000-0000-000000000012', 'mock', 'credit_card', 'confirmed', 159.90),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000013', 'b1000000-0000-0000-0000-000000000013', 'mock', 'pix',         'confirmed', 670.00),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000014', 'b1000000-0000-0000-0000-000000000014', 'mock', 'credit_card', 'pending',   199.90),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000015', 'b1000000-0000-0000-0000-000000000015', 'mock', 'debit_card',  'pending',   349.00),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000016', 'b1000000-0000-0000-0000-000000000016', 'mock', 'pix',         'pending',   499.00),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000017', 'b1000000-0000-0000-0000-000000000017', 'mock', 'credit_card', 'confirmed', 125.00),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000018', 'b1000000-0000-0000-0000-000000000018', 'mock', 'pix',         'confirmed', 239.90),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000019', 'b1000000-0000-0000-0000-000000000019', 'mock', 'credit_card', 'pending',    89.90),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000020', 'b1000000-0000-0000-0000-000000000020', 'mock', 'pix',         'pending',   415.00),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000021', 'b1000000-0000-0000-0000-000000000021', 'mock', 'debit_card',  'failed',    199.90),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000022', 'b1000000-0000-0000-0000-000000000022', 'mock', 'credit_card', 'failed',     89.90),
  (gen_random_uuid(), 'd1000000-0000-0000-0000-000000000023', 'b1000000-0000-0000-0000-000000000023', 'mock', 'pix',         'failed',    320.00)
ON CONFLICT DO NOTHING;

COMMIT;

SELECT 'Customers' AS tabela, COUNT(*) FROM customer WHERE role = 'customer'
UNION ALL SELECT 'Orders',   COUNT(*) FROM "order"
UNION ALL SELECT 'Items',    COUNT(*) FROM order_item
UNION ALL SELECT 'Payments', COUNT(*) FROM payment;