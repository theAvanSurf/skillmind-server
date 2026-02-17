import { Column, Entity, PrimaryGeneratedColumn } from "typeorm";

@Entity()
export class User {
    @PrimaryGeneratedColumn('uuid')
    id!: string;
    @Column()
    firstName!: string
    @Column()
    lastName!: string
    @Column({unique: true})
    email!: string
    @Column()
    password_hash!: string
    @Column({ type: 'timestamp', default: () => 'CURRENT_TIMESTAMP' })
    created_at!: Date
    @Column({ type: 'timestamp', default: () => 'CURRENT_TIMESTAMP', onUpdate: 'CURRENT_TIMESTAMP' })
    updated_at!: Date
    @Column({default: false})
    is_verified!: boolean
}