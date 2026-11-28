FROM postgres:18-trixie@sha256:64c8b0d31e4ab6ea665e8426ea406a94bf52de3c05b3a4a8bf713704eefa9328
RUN apt-get update -qq \
    && apt-get install -y --no-install-recommends \
    postgresql-18-postgis-3 \
    postgresql-18-postgis-3-scripts \
    postgresql-18-pgvector \
    && rm -rf /var/lib/apt/lists/*
USER postgres
HEALTHCHECK --interval=10s --timeout=5s --start-period=10s --retries=5 \
    CMD ["pg_isready", "-U", "postgres"]
