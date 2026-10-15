-- =============================================================================
-- COMPRAI — Seed Dinâmico
-- Gera dados aleatórios a cada execução (30 clientes, 60 pedidos)
-- Uso: psql -h <host> -U <user> -d <db> -f seed.sql
-- =============================================================================

DO $$
DECLARE
  -- Arrays de nomes
  primeiros TEXT[] := ARRAY[
    'Ana','Bruno','Carla','Diego','Elisa','Fabio','Gabriela','Henrique',
    'Isabela','João','Katia','Lucas','Marina','Nicolas','Olivia','Pedro',
    'Rafaela','Samuel','Tatiana','Victor','Yasmin','Rodrigo','Camila','Felipe'
  ];
  sobrenomes TEXT[] := ARRAY[
    'Silva','Santos','Oliveira','Souza','Rodrigues','Ferreira','Alves',
    'Pereira','Lima','Carvalho','Costa','Martins','Araújo','Melo',
    'Barbosa','Ribeiro','Rocha','Campos','Gomes','Mendes','Castro','Cunha'
  ];

  -- Localidades (cidade, estado, CEP prefixo)
  localidades TEXT[][] := ARRAY[
    ARRAY['São Paulo','SP','01'], ARRAY['Rio de Janeiro','RJ','20'],
    ARRAY['Belo Horizonte','MG','30'], ARRAY['Curitiba','PR','80'],
    ARRAY['Porto Alegre','RS','90'], ARRAY['Brasília','DF','70'],
    ARRAY['Salvador','BA','40'], ARRAY['Recife','PE','50'],
    ARRAY['Fortaleza','CE','60'], ARRAY['Manaus','AM','69'],
    ARRAY['Campinas','SP','13'], ARRAY['Florianópolis','SC','88']
  ];

  ruas TEXT[] := ARRAY[
    'Rua das Flores','Av. Paulista','Rua XV de Novembro','Av. Brasil',
    'Rua das Acácias','Av. Independência','Rua das Palmeiras',
    'Rua Sete de Setembro','Av. Getúlio Vargas','Rua Dom Pedro II',
    'Rua do Comércio','Av. das Américas','Rua Marechal Deodoro'
  ];

  -- Produtos (nome, sku, faixa de preço min, max)
  produtos TEXT[][] := ARRAY[
    ARRAY['Smartphone Samsung Galaxy A55','SM-A55','899','2499'],
    ARRAY['Notebook Dell Inspiron 15','NB-DL15','2999','4599'],
    ARRAY['Fone Sony WH-1000XM5','FO-SN1000','899','1499'],
    ARRAY['Smart TV LG 55" OLED','TV-LG55','3499','5999'],
    ARRAY['iPad Air 11" M2','TB-APLA11','4299','6499'],
    ARRAY['Câmera Canon EOS R50','CM-CNEOR','3199','4299'],
    ARRAY['Monitor LG UltraWide 34"','MN-LG34','1899','2999'],
    ARRAY['Teclado Mecânico Keychron K2','TC-KCH2','599','899'],
    ARRAY['Mouse Logitech MX Master 3','MS-LGMX3','499','749'],
    ARRAY['SSD Samsung 990 Pro 1TB','HD-SM1TB','399','699'],
    ARRAY['Placa de Vídeo RTX 4070','GPU-NV70','2899','4499'],
    ARRAY['Processador Intel Core i7-14700','CPU-IN147','1299','1899'],
    ARRAY['Memória RAM Corsair 32GB DDR5','RAM-CS32','599','999'],
    ARRAY['Impressora HP LaserJet Pro','IM-HP401','799','1299'],
    ARRAY['Roteador Wi-Fi 6 ASUS','RT-AS86U','699','1099'],
    ARRAY['Caixinha JBL Charge 5','SP-JBL5','599','899'],
    ARRAY['Smartwatch Apple Watch S9','SW-APW9','2999','4499'],
    ARRAY['Console PS5 Digital','GC-PS5D','2999','3799'],
    ARRAY['Controle Xbox Wireless','GC-XBXW','349','499'],
    ARRAY['Câmera de Segurança Intelbras','CM-IT4MP','249','449']
  ];

  statuses TEXT[] := ARRAY[
    'pending','pending',
    'confirmed','confirmed',
    'processing','processing',
    'shipped','shipped',
    'delivered','delivered','delivered',
    'cancelled'
  ];

  metodos_pag TEXT[] := ARRAY['pix','pix','credit_card','credit_card','debit_card'];
  canais      TEXT[] := ARRAY['web','web','web','whatsapp','mobile'];

  -- Variáveis de controle
  num_clientes INT := 30;
  num_pedidos  INT := 60;

  v_cust_id    UUID;
  v_order_id   UUID;
  v_loc        TEXT[];
  v_prod_nome  TEXT;
  v_prod_sku   TEXT;
  v_prod_min   NUMERIC;
  v_prod_max   NUMERIC;
  v_primeiro   TEXT;
  v_sobrenome TEXT;
  v_nome      TEXT;
  v_email     TEXT;
  v_status    TEXT;
  v_metodo    TEXT;
  v_pay_status TEXT;
  v_total     NUMERIC(12,2);
  v_created   TIMESTAMPTZ;
  v_itens     INT;
  v_preco     NUMERIC(12,2);
  v_qty       INT;
  v_idx_p     INT;
  v_idx_l     INT;
  cust_ids    UUID[];
  i INT; j INT;

BEGIN
  -- ── Limpar dados existentes ──────────────────────────────────────────────────
  DELETE FROM payment;
  DELETE FROM order_item;
  DELETE FROM "order";
  DELETE FROM customer;
  RAISE NOTICE '  Dados anteriores removidos.';

  -- ── Clientes ────────────────────────────────────────────────────────────────
  FOR i IN 1..num_clientes LOOP
    v_primeiro  := primeiros  [1 + (floor(random() * array_length(primeiros,  1)))::INT % array_length(primeiros,  1)];
    v_sobrenome := sobrenomes [1 + (floor(random() * array_length(sobrenomes, 1)))::INT % array_length(sobrenomes, 1)];
    v_nome      := v_primeiro || ' ' || v_sobrenome;
    v_email     := lower(regexp_replace(v_primeiro, '[^a-zA-Z]', '', 'g'))
                || '.'
                || lower(regexp_replace(v_sobrenome, '[^a-zA-Z]', '', 'g'))
                || i::TEXT || '@email.com';

    INSERT INTO customer (
      id, name, email, phone, role, email_verified, totp_enabled, channel, created_at, updated_at
    ) VALUES (
      gen_random_uuid(),
      v_nome,
      v_email,
      '(' || (11 + (floor(random() * 88))::INT)::TEXT || ') 9'
        || lpad((floor(random() * 90000000 + 10000000))::TEXT, 8, '0'),
      'customer',
      random() > 0.25,
      random() > 0.7,
      canais[1 + (floor(random() * array_length(canais, 1)))::INT % array_length(canais, 1)],
      NOW() - (random() * INTERVAL '120 days'),
      NOW()
    ) RETURNING id INTO v_cust_id;

    cust_ids := array_append(cust_ids, v_cust_id);
  END LOOP;
  RAISE NOTICE '  % clientes inseridos.', num_clientes;

  -- ── Pedidos ──────────────────────────────────────────────────────────────────
  FOR i IN 1..num_pedidos LOOP
    v_cust_id := cust_ids[1 + (floor(random() * array_length(cust_ids, 1)))::INT % array_length(cust_ids, 1)];
    v_status  := statuses[1 + (floor(random() * array_length(statuses, 1)))::INT % array_length(statuses, 1)];
    v_loc     := localidades[1 + (floor(random() * array_length(localidades, 1)))::INT % array_length(localidades, 1)];
    v_created := NOW() - (random() * INTERVAL '90 days');
    v_total   := 0;
    v_order_id := gen_random_uuid();

    INSERT INTO "order" (
      id, idempotency_key, customer_id, status, total_amount,
      shipping_zip, shipping_street, shipping_number, shipping_city, shipping_state,
      tracking_code,
      created_at, updated_at
    ) VALUES (
      v_order_id,
      gen_random_uuid(),
      v_cust_id,
      v_status,
      0,
      v_loc[3] || lpad((floor(random() * 999))::TEXT, 3, '0') || '-'
        || lpad((floor(random() * 999))::TEXT, 3, '0'),
      ruas[1 + (floor(random() * array_length(ruas, 1)))::INT % array_length(ruas, 1)],
      (floor(random() * 9999 + 1))::TEXT,
      v_loc[1],
      v_loc[2],
      CASE WHEN v_status IN ('shipped','delivered')
           THEN 'BR' || lpad((floor(random() * 999999999))::TEXT, 9, '0') || 'BR'
           ELSE NULL END,
      v_created,
      v_created
    );

    -- Itens do pedido
    v_itens := 1 + (floor(random() * 4))::INT;
    FOR j IN 1..v_itens LOOP
      v_idx_p    := 1 + (floor(random() * array_length(produtos, 1)))::INT % array_length(produtos, 1);
      v_prod_nome := produtos[v_idx_p][1];
      v_prod_sku  := produtos[v_idx_p][2];
      v_prod_min  := produtos[v_idx_p][3]::NUMERIC;
      v_prod_max  := produtos[v_idx_p][4]::NUMERIC;
      v_qty   := 1 + (floor(random() * 3))::INT;
      v_preco := v_prod_min + floor(random() * (v_prod_max - v_prod_min))::NUMERIC;
      v_total := v_total + (v_qty * v_preco);

      INSERT INTO order_item (
        order_id, product_id, product_title, sku, quantity, unit_price, source, created_at
      ) VALUES (
        v_order_id,
        gen_random_uuid()::TEXT,
        v_prod_nome,
        v_prod_sku,
        v_qty,
        v_preco,
        (ARRAY['MercadoLivre','Shopify','FakeCatalog'])[1 + (floor(random() * 3))::INT % 3],
        v_created
      );
    END LOOP;

    -- Atualiza total
    UPDATE "order" SET total_amount = v_total WHERE id = v_order_id;

    -- Pagamento
    v_metodo := metodos_pag[1 + (floor(random() * array_length(metodos_pag, 1)))::INT % array_length(metodos_pag, 1)];
    v_pay_status := CASE
      WHEN v_status IN ('delivered','shipped','processing') THEN 'confirmed'
      WHEN v_status = 'cancelled' THEN
        CASE WHEN random() > 0.5 THEN 'failed' ELSE 'confirmed' END
      ELSE
        CASE WHEN random() > 0.3 THEN 'confirmed' ELSE 'pending' END
    END;

    INSERT INTO payment (
      id, idempotency_key, order_id, provider, method, status, amount,
      paid_at, created_at, updated_at
    ) VALUES (
      gen_random_uuid(),
      gen_random_uuid(),
      v_order_id,
      'mock',
      v_metodo,
      v_pay_status,
      v_total,
      CASE WHEN v_pay_status = 'confirmed' THEN v_created + INTERVAL '3 minutes' ELSE NULL END,
      v_created,
      v_created
    );
  END LOOP;
  RAISE NOTICE '  % pedidos inseridos.', num_pedidos;

  RAISE NOTICE 'Seed concluído! Clientes: %, Pedidos: %', num_clientes, num_pedidos;
END $$;
