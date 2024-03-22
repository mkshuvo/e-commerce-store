// wishlist.entity.ts

import { Entity, PrimaryGeneratedColumn, Column, ManyToOne } from 'typeorm';
import { User } from './user.entity';
import { Product } from './product.entity';

@Entity()
export class Wishlist {
  @PrimaryGeneratedColumn()
  wishlist_id: number;

  @ManyToOne(() => User, user => user.wishlists)
  user: User;

  @ManyToOne(() => Product, product => product.wishlists)
  product: Product;
}
