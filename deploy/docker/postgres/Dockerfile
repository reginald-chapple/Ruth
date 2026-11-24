FROM apache/age:release_PG18_1.8.0@sha256:47a0b054c3663a3e64a25fc0c5a982f1b58348019274b326a691cdf77f78eb58
RUN apt-get update -qq \
    && apt-get install -y --no-install-recommends \
    postgresql-18-postgis-3=3.6.4+dfsg-2.pgdg13+1 \
    postgresql-18-postgis-3-scripts=3.6.4+dfsg-2.pgdg13+1 \
    postgresql-18-pgvector=0.8.7-1.pgdg13+1 \
    && rm -rf /var/lib/apt/lists/*
