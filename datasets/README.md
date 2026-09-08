# Datasets — Comprai

Arquivos de dados para popular lojas de teste e validar os plugins de catálogo.

## Estrutura

```
datasets/
  shopify/
    comprai-produtos-br.zip        — 40 produtos BR fictícios (Eletrônicos, Moda, Casa, Beleza, Esportes)
    amazon-products-sample.zip     — amostra do Amazon Products 2023 (Kaggle) — pendente V008
  templates/
    shopify-sample.zip             — template oficial de importação CSV do Shopify
    woocommerce-sample.zip         — pendente V008
    vtex-sample.zip                — pendente V008
```

## Como usar

### comprai-produtos-br.zip
Gerado via Bogus BR com marcas, categorias e preços em BRL.
Importar em: Shopify Admin → Products → Import → Upload a Shopify-formatted CSV file.

### amazon-products-sample.zip
Amostra do dataset público Amazon Products 2023 (1.4M produtos).
Fonte: https://www.kaggle.com/datasets/asaniczka/amazon-products-dataset-2023-1-4m-products
Dataset completo disponível na VPS em /home/projetos/comprai/datasets/amazon/
Transformar via: POST /api/catalog/import?platform=amazon-csv

## Geração programática

```bash
# Gerar novo CSV e zipar
POST /api/catalog/generate?qty=200&category=eletronicos&format=shopify-csv
```
