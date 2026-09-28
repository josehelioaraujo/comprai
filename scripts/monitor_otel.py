#!/usr/bin/env python3
import subprocess, time, datetime, os

def log(msg):
    ts = datetime.datetime.now().strftime("%H:%M:%S")
    print(f"[{ts}] {msg}", flush=True)

log("=== Monitor OTel Collector iniciado (Ctrl+C para parar) ===")
last_lines = set()

while True:
    try:
        # Logs do collector
        result = subprocess.run(
            ["docker", "logs", "comprai-otel-collector", "--tail=15", "--since=10s"],
            capture_output=True, text=True
        )
        lines = (result.stdout + result.stderr).strip().splitlines()
        new_lines = [l for l in lines if l not in last_lines]
        
        if new_lines:
            for l in new_lines:
                # Destacar eventos importantes
                if "error" in l.lower():
                    print(f"  ❌ {l}", flush=True)
                elif "exported" in l.lower() or "sent" in l.lower() or "success" in l.lower():
                    print(f"  ✅ {l}", flush=True)
                elif "receiving" in l.lower() or "traces" in l.lower() or "metrics" in l.lower():
                    print(f"  📡 {l}", flush=True)
                else:
                    print(f"  {l}", flush=True)
            last_lines = set(lines[-30:])
        else:
            log("(sem novos logs...)")
        
        # Status dos containers
        result2 = subprocess.run(
            ["docker", "ps", "--filter", "name=comprai", "--format", "{{.Names}}|{{.Status}}"],
            capture_output=True, text=True
        )
        containers = result2.stdout.strip().splitlines()
        running = [c.split("|")[0] for c in containers if "Up" in c]
        log(f"Containers UP: {', '.join(running)}")
        
        time.sleep(5)
    except KeyboardInterrupt:
        log("Monitor encerrado.")
        break
    except Exception as e:
        log(f"Erro: {e}")
        time.sleep(5)
