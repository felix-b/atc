FROM mmozeiko/mingw-w64:latest

RUN apt-get update && apt-get install -y git
RUN sed -i -e 's/http:\/\/archive.ubuntu.com\/ubuntu\//mirror:\/\/mirrors.ubuntu.com\/mirrors.txt/' /etc/apt/sources.list && apt-get update
RUN apt-get install -y --no-install-recommends freeglut3-dev

VOLUME /src

RUN echo "" | adduser --uid 1000 --disabled-password  --gecos "" builduser && adduser builduser sudo
RUN echo '%sudo ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

ADD build.sh /usr/bin/build.sh
RUN chmod +x /usr/bin/build.sh

USER builduser
WORKDIR /src
ENTRYPOINT ["build.sh"]
