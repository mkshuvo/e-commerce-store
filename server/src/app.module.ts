import { Module } from '@nestjs/common';
import { TypeOrmModule } from '@nestjs/typeorm';
import { User } from './entities/user.entity';
import { Product } from './entities/product.entity';
import { Cart } from './entities/cart.entity';
import { Wishlist } from './entities/wishlist.entity';
import { CustomerSupport } from './entities/customer-support.entity';
import { AppController } from './app.controller';
import { AppService } from './app.service';

@Module({
  imports: [
    TypeOrmModule.forRoot({
      type: 'postgres',
      host: 'postgres', 
      port: 5432, 
      username: 'admin',
      password: 'password',
      database: 'ecommerce_db',
      entities: [User, Product, Cart, Wishlist, CustomerSupport],
      synchronize: true, 
    }),
  ],
  controllers: [AppController],
  providers: [AppService],
})
export class AppModule {}
