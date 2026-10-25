#!/usr/bin/env python3
"""
Gera architecture.md com diagrama Mermaid e resumo de componentes
lendo os .csproj reais do repositório — usado como insumo para agentes.
"""
import os, re, json
from pathlib import Path
from datetime import datetime, timezone
from xml.etree import ElementTree as ET

ROOT = Path(__file__).parent.parent.parent

# Categorias de projetos
CATEGORIES = {
    "api":      ["UcpAgent.Api", "UcpAgent.McpServer"],
    "app":      ["UcpAgent.Application"],
    "domain":   ["UcpAgent.Domain"],
    "infra":    ["UcpAgent.Infrastructure"],
    "kernel":   ["UcpAgent.SharedKernel"],
    "plugins":  [],  # detectados automaticamente
    "tests":    [],  # detectados automaticamente
    "frontend": ["web", "mobile", "mobile-v2", "admin"],
}

LABELS = {
    "api":      "🌐 Apresentação",
    "app":      "⚙️ Aplicação (CQRS)",
    "domain":   "🧬 Domínio",
    "infra":    "🔧 Infraestrutura",
    "kernel":   "🔗 SharedKernel",
    "plugins":  "🔌 Plugins",
    "tests":    "🧪 Testes",
    "frontend": "💻 Frontend",
}

def get_category(name):
    for cat, names in CATEGORIES.items():
        if name in names:
            return cat
    if "Tests" in name or "test" in name.lower():
        return "tests"
    if any(p in name for p in ["Catalog.", "PriceWatcher", "FakeCatalog"]):
        return "plugins"
    return "infra"

def short(name):
    return name.replace("UcpAgent.", "").replace(".Tests", "Tests")

def parse_csproj(path):
    try:
        tree = ET.parse(path)
        root = tree.getroot()
        refs = []
        for ref in root.iter("ProjectReference"):
            inc = ref.get("Include", "")
            dep = Path(inc.replace("\\", "/")).stem
            refs.append(dep)
        return refs
    except Exception:
        return []

# Coleta todos os projetos
projects = {}
for csproj in ROOT.rglob("*.csproj"):
    if any(p in str(csproj) for p in ["obj", "bin"]):
        continue
    name = csproj.stem
    deps = parse_csproj(csproj)
    cat = get_category(name)
    projects[name] = {"deps": deps, "cat": cat, "path": str(csproj.relative_to(ROOT))}

# Agrupa por categoria
by_cat = {}
for name, info in projects.items():
    c = info["cat"]
    by_cat.setdefault(c, []).append(name)

# Gera Mermaid
lines = ["```mermaid", "graph TD"]

# Subgraphs por camada (exceto tests)
order = ["api", "app", "domain", "infra", "kernel", "plugins", "frontend"]
for cat in order:
    members = by_cat.get(cat, [])
    if not members:
        continue
    label = LABELS[cat]
    lines.append(f'  subgraph {cat}["{label}"]')
    for m in sorted(members):
        lines.append(f'    {short(m)}["{short(m)}"]')
    lines.append("  end")

# Arestas (só entre src, sem tests)
for name, info in projects.items():
    if info["cat"] == "tests":
        continue
    for dep in info["deps"]:
        if dep in projects and projects[dep]["cat"] != "tests":
            lines.append(f"  {short(name)} --> {short(dep)}")

# Estilos por camada
styles = {
    "api":     "#4A90E2",
    "app":     "#7B68EE",
    "domain":  "#50C878",
    "infra":   "#FF6B6B",
    "kernel":  "#FFD93D",
    "plugins": "#FF9F43",
    "frontend":"#26de81",
}
for cat, color in styles.items():
    for m in by_cat.get(cat, []):
        lines.append(f"  style {short(m)} fill:{color},color:#fff")

lines.append("```")
mermaid = "\n".join(lines)

# Resumo de componentes como JSON (para agentes)
summary = {
    "generated_at": datetime.now(timezone.utc).isoformat(),
    "total_projects": len(projects),
    "by_category": {cat: sorted([short(n) for n in names]) for cat, names in by_cat.items()},
    "dependencies": {
        short(n): [short(d) for d in info["deps"] if d in projects]
        for n, info in projects.items()
        if info["cat"] != "tests"
    }
}

# Portas (lê SharedKernel)
ports = []
sk_path = ROOT / "src" / "UcpAgent.SharedKernel"
if sk_path.exists():
    for cs in sk_path.rglob("*.cs"):
        content = cs.read_text(encoding="utf-8", errors="ignore")
        found = re.findall(r"interface\s+(I\w+Port)", content)
        ports.extend(found)
ports = sorted(set(ports))
summary["ports"] = ports

# Feature flags
flags = []
appsettings = ROOT / "src" / "UcpAgent.Api" / "appsettings.json"
if appsettings.exists():
    try:
        data = json.loads(appsettings.read_text())
        flags = list(data.get("FeatureFlags", {}).keys())
    except Exception:
        pass
summary["feature_flags"] = flags

commit = os.environ.get("GITHUB_SHA", "local")[:8]
run = os.environ.get("GITHUB_RUN_NUMBER", "0")

# Gera architecture.md
md = f"""# 🏗️ Architecture Map — Comprai

> Gerado automaticamente em `{datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M')} UTC`  
> Commit: `{commit}` · Build: `#{run}`  
> **Fonte de verdade para agentes** — leia antes de qualquer tarefa de código.

---

## Diagrama de Dependências entre Projetos

{mermaid}

---

## Componentes por Camada

| Camada | Projetos |
|--------|----------|
""" + "\n".join(
    f"| {LABELS.get(cat, cat)} | {', '.join(f'`{short(n)}`' for n in sorted(names))} |"
    for cat, names in by_cat.items()
    if names
) + f"""

---

## Portas Hexagonais (SharedKernel)

{chr(10).join(f'- `{p}`' for p in ports)}

---

## Feature Flags

{chr(10).join(f'- `{f}`' for f in flags) if flags else '_Não encontradas em appsettings.json_'}

---

## Para Agentes

- **Grafo interativo:** [Knowledge Graph](https://josehelioaraujo.github.io/comprai/)
- **Dados brutos:** [graph.json](https://josehelioaraujo.github.io/comprai/graph.json)
- **Este arquivo:** [architecture.md](https://josehelioaraujo.github.io/comprai/architecture.md)
- **Resumo JSON:** [architecture.json](https://josehelioaraujo.github.io/comprai/architecture.json)

> Use `architecture.json` para parsing programático em skills/agentes.
"""

out = Path(os.environ.get("OUTPUT_DIR", "pages-out"))
out.mkdir(parents=True, exist_ok=True)
(out / "architecture.md").write_text(md, encoding="utf-8")
(out / "architecture.json").write_text(json.dumps(summary, indent=2, ensure_ascii=False), encoding="utf-8")

print(f"✅ {len(projects)} projetos | {len(ports)} portas | {len(flags)} feature flags")
print(f"   → pages-out/architecture.md")
print(f"   → pages-out/architecture.json")
