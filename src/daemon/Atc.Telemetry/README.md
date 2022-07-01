```
docker run --name jaeger \
> -e COLLECTOR_OTLP_ENABLED=true \
> -p 16686:16686 \
> -p 4317:4317 \
> -p 4318:4318 \
> --platform linux/arm64/v8 \
> jaegertracing/all-in-one:1.35
```