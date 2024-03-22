// cart.entity.ts

import { Entity, PrimaryGeneratedColumn, Column, ManyToOne } from 'typeorm';
import { User } from './user.entity';
import { Product } from './product.entity';

@Entity()
export class Cart {
  @PrimaryGeneratedColumn()
  cart_id: number;

  @ManyToOne(() => User, user => user.carts)
  user: User;

  @ManyToOne(() => Product, product => product.carts)
  product: Product;

  @Column()
  quantity: number;
}
