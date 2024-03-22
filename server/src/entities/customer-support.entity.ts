// customer-support.entity.ts

import { Entity, PrimaryGeneratedColumn, Column, ManyToOne } from 'typeorm';
import { User } from './user.entity';

@Entity()
export class CustomerSupport {
  @PrimaryGeneratedColumn()
  support_id: number;

  @ManyToOne(() => User, user => user.supportTickets)
  user: User;

  @Column('text')
  issue_description: string;

  @Column({ length: 20 })
  status: string;
}
