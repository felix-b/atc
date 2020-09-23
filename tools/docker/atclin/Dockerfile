FROM ubuntu:18.04

RUN apt-get update
RUN apt-get -y install g++
RUN apt-get -y install build-essential
RUN apt-get -y install git
RUN apt-get install -y --no-install-recommends bash coreutils ca-certificates curl

# get latest cmake
RUN apt-get -y --no-install-recommends install apt-transport-https gnupg software-properties-common wget \
 && wget -O - https://apt.kitware.com/keys/kitware-archive-latest.asc 2>/dev/null | gpg --dearmor - | tee /etc/apt/trusted.gpg.d/kitware.gpg >/dev/null \
 && apt-add-repository 'deb https://apt.kitware.com/ubuntu/ xenial main' \
 && apt-get update \
 && apt-get -y --no-install-recommends install cmake

# install dependency libs
RUN apt-get install -y --no-install-recommends freeglut3-dev libudev-dev libopenal-dev g++-multilib gcc-multilib libspeechd-dev && apt-get clean
RUN apt-get install -y --no-install-recommends python && apt-get clean

VOLUME /src

ADD build-all.sh /usr/bin/build-all.sh
RUN chmod +x /usr/bin/build-all.sh

WORKDIR /src
ENTRYPOINT ["build-all.sh"]
