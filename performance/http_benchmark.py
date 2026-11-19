"""Prueba local de lectura HTTP, sin dependencias ni cambios en la base de datos."""
import argparse
import concurrent.futures
from contextlib import closing
import csv
import datetime
import http.client
import json
import math
from pathlib import Path
import platform
import re
import statistics
import time
from urllib.parse import urlsplit


def percentile(values, p):
    return sorted(values)[max(0, math.ceil(len(values) * p) - 1)]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base-url', default='http://127.0.0.1:5187')
    parser.add_argument('--requests', type=int, default=100)
    parser.add_argument('--repeats', type=int, default=2)
    parser.add_argument('--output', default='performance/results.json')
    args = parser.parse_args()
    target = urlsplit(args.base_url)
    if target.scheme != 'http' or target.hostname not in ('localhost', '127.0.0.1'):
        parser.error('Utiliza una URL HTTP local para esta prueba.')
    if args.requests < 10 or args.repeats < 1:
        parser.error('Se requieren al menos 10 peticiones y una repetición.')

    def connection():
        return closing(http.client.HTTPConnection(target.hostname, target.port or 80, timeout=15))

    def get(conn, path):
        start = time.perf_counter()
        try:
            conn.request('GET', path, headers={'Accept': 'text/html'})
            response = conn.getresponse()
            body = response.read()
            return (time.perf_counter() - start) * 1000, response.status, len(body), body
        except (OSError, http.client.HTTPException) as error:
            conn.close()
            return (time.perf_counter() - start) * 1000, type(error).__name__, 0, b''

    endpoints = ['/Consultas/Index', '/Consultas/Create', '/Mascotas/Index', '/Citas/Index']
    cold = []
    # Las solicitudes previas permiten verificar rutas, sin seguir redirecciones al login.
    with connection() as conn:
        for path in list(endpoints):
            latency, status, size, body = get(conn, path)
            if status != 200:
                raise RuntimeError(f'{path}: {status}. No se medirá una página fallida o una redirección.')
            cold.append({'path': path, 'first_observed_ms': round(latency, 3), 'bytes': size})
            if path == '/Consultas/Index':
                match = re.search(rb'/Consultas/Details/(\d+)', body)
                if match:
                    endpoints.append('/Consultas/Details/' + match[1].decode())

    results = []
    raw = []
    for path in endpoints:
        with connection() as conn:
            for _ in range(5):
                _, status, _, _ = get(conn, path)
                if status != 200:
                    raise RuntimeError(f'Falló calentamiento: {path}, {status}')
        for concurrency in (1, 5, 10):
            for repeat in range(1, args.repeats + 1):
                def worker(count):
                    with connection() as conn:
                        return [get(conn, path)[:3] for _ in range(count)]

                counts = [args.requests // concurrency + (i < args.requests % concurrency)
                          for i in range(concurrency)]
                start = time.perf_counter()
                with concurrent.futures.ThreadPoolExecutor(max_workers=concurrency) as pool:
                    samples = [sample for batch in pool.map(worker, counts) for sample in batch]
                elapsed = time.perf_counter() - start
                latencies = [s[0] for s in samples]
                errors = sum(s[1] != 200 for s in samples)
                result = dict(path=path, concurrency=concurrency, repeat=repeat,
                              requests=len(samples), errors=errors,
                              elapsed_s=round(elapsed, 3),
                              requests_per_s=round(len(samples) / elapsed, 2),
                              mean_ms=round(statistics.mean(latencies), 3),
                              p50_ms=round(percentile(latencies, .50), 3),
                              p95_ms=round(percentile(latencies, .95), 3),
                              p99_ms=round(percentile(latencies, .99), 3),
                              max_ms=round(max(latencies), 3),
                              mean_bytes=round(statistics.mean(s[2] for s in samples)))
                results.append(result)
                raw.extend(dict(path=path, concurrency=concurrency, repeat=repeat,
                                latency_ms=round(s[0], 3), status=s[1], bytes=s[2]) for s in samples)
                print(json.dumps(result), flush=True)

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    report = dict(timestamp_utc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
                  base_url=args.base_url, python=platform.python_version(),
                  os=platform.platform(), requests_per_batch=args.requests,
                  repeats=args.repeats, warmup_per_endpoint=5,
                  first_observed=cold, results=results)
    output.write_text(json.dumps(report, indent=2), encoding='utf-8')
    with output.with_suffix('.csv').open('w', newline='', encoding='utf-8') as file:
        writer = csv.DictWriter(file, fieldnames=list(raw[0]))
        writer.writeheader()
        writer.writerows(raw)
    if any(r['errors'] for r in results):
        raise SystemExit('Se observaron errores; revisar resultados antes de interpretar tiempos.')


if __name__ == '__main__':
    main()
