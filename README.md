# FusionCacheOpenTelemetrySample

Quick sample to play a bit with FusionCache, as well as its OpenTelemetry integration.

## Trying things out

- Start Grafana, Prometheus and friends with `docker-compose up -d`
- Start the API with `dotnet run --project ./src/Api`
- Do some requests to the API at `http://localhost:8080/some-key`
- Or hammer the API using the k6 script `k6 run --vus 10 --duration 30s -e MAX_NUM_KEYS=25 ./k6/script.js`
- Check the metrics at `http://localhost:3000` (user:pass => admin:admin)
- In the `grafana` folder, there's a simple dashboard that can be imported in Grafana to have a quick look at some metrics