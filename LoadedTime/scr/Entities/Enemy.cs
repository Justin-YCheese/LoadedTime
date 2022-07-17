using Godot;
using System;
using Entities;
using Players;

public class Enemy : Entity
{
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        moveSpeed = 250;
        gravity = 1000;

        SetPhysicsProcess(false); //Enemy won't start moving when starting off screen

        velocity.x = -moveSpeed;

        thisType = entityType.enemy;

        GD.Print("Hello Enemy");
    }

    public void onStompDetector_PlayerStomped(PhysicsBody2D player){
        if(player.GlobalPosition.y < GetNode<Area2D>("StompDetector").GlobalPosition.y){
            //enemyStomped?.Invoke();
            GetNode<CollisionShape2D>("EnemyCollision").Disabled = true;
            QueueFree();
            return;
        }
        return;
    }

    public override void _PhysicsProcess (float delta)
    {        
        if(IsOnWall())
            velocity.x *= -1;

        velocity.y += gravity * delta;

        velocity.y = MoveAndSlide(velocity,FLOOR_NORMAL).y;
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
