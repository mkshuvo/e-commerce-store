FROM node:20-alpine

WORKDIR /app

COPY package*.json ./

RUN npm install -g @nestjs/cli

RUN npm install

COPY . .

EXPOSE 3005

ENV NODE_ENV=development

CMD ["npm", "run", "start:dev"]
