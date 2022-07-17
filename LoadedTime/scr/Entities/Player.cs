using Godot;
using System;
using Entities;

namespace Players
{
public class Player : Entity
{
    /* From Entity
    Vector2     Velocity

    enum        entityType
    entityType  thisType
    
    moveSpeed
    gravity

    Vector2 velocity = Vector2.Zero;
    */

    // [Export]
    // public int moveSpeed = 400; // pixels/s
    // [Export]
    // public int gravity = 2000; // pixels/s^2
    [Export]
    public int jumpSpeed = 700; // pix/s
    [Export]
    public float jumpGravityModifier = 0.6f;
    [Export]
    public float downGravityModifier = 1.6f;
    [Export]
    public int minimumStompBounce = 400;
    [Export]
    public int quickFallInitialSpeed = 300;
    [Export]
    public float quickFallModifier = 1f;

    // public delegate void delegateEnemyStomped();
    // public event delegateEnemyStomped enemyStomped;
    
    //********************************************************************//
    //                                                                    //
    //                               States                               //
    //                                                                    //
    //********************************************************************//
    private enum playerState {
        None        = 0b_0000_0000,
        Grounded    = 0b_0000_0001,
        Jumping     = 0b_0000_0010,
        Aerial      = 0b_0000_0011,
        Rolling     = 0b_0000_0100,
        // Vaulting = 0b_0000_1000,
        Dashing     = 0b_0000_1100,
        Grabbing    = 0b_0001_0000,
        Aiming      = 0b_0010_0000,
        Shooting    = 0b_0011_0000,
        Action = Grounded | Jumping | Aerial,
        Dodge = Rolling | Dashing,
        Strategy = Grabbing | Aiming | Shooting
    }
    // Variables
    private playerState currentState = playerState.Aerial;
    private playerState previousState = playerState.None;
    private playerState storedState = playerState.None; //When going into Strategy States and the returning
    // Functions
    private bool ifAction(playerState state){
        if((state & playerState.Action) != 0b_0000_0000) return true; return false;
    }
    private bool ifDodge(playerState state){
        if((state & playerState.Dodge) != 0b_0000_0000) return true; return false;
    }
    private bool ifStrategy(playerState state){
        if((state & playerState.Strategy) != 0b_0000_0000) return true; return false;
    }

    // Player Motion
    //Motion velocity = new Motion();

    // Player Physics
    [Export]
    public float coyoteTime = 0.5f; // seconds of cyotyeTime
    [Export]
    public float drag = 0.05f; // drag coefficent 

    // For cyoteTime
    //private double timeSinceGrounded = 0;

    // For testing
    //private bool processUsedFlag = false;
    //private bool physicsUsedFlag = false;

    public override void _Ready()
    {        
        thisType = entityType.player;

        // enemyStomped += onEnemyStomped;

        moveSpeed = 400;
        gravity = 2000;

        GD.Print("Hello Player");
    }

    //********************************************************************//
    //                                                                    //
    //                          Signals / Events                          //
    //                                                                    //
    //********************************************************************//
    
    public void onEnemyDetector_AreaEntered(Area2D enemyStompDetector){
        if(GlobalPosition.y < enemyStompDetector.GlobalPosition.y)
            velocity.y = Math.Min(-velocity.y,-minimumStompBounce);
        return;
    }

    // public void onEnemyStomped(){
    //     velocity.y = Math.Min(-velocity.y,-minimumStompBounce);    
    // }

    //********************************************************************//
    //                                                                    //
    //                               Process                              //
    //                                                                    //
    //********************************************************************//
    public override void _PhysicsProcess (float delta){
        printStateChange(); // If the State changes this will print the new state
        processState();
        processPhysics();
    }


    //********************************************************************//
    //                                                                    //
    //                                State                               //
    //                                                                    //
    //********************************************************************//
    public void processState(){
        switch (currentState) {
            case playerState.Grounded: // * ~ * ~ * ~ * Grounded * ~ * ~ * ~ * //
            {
                /*
                Aerial <-
                Rolling <-
                -> Jumping
                -> Aerial
                -> Rolling
                */

                // Jumping
                if(Input.IsActionJustPressed("jump")){
                    currentState = playerState.Jumping;
                }

                // Aerial 
                if(!IsOnFloor()){
                    currentState = playerState.Aerial;
                }

                // Rolling

                break;
            }
            case playerState.Jumping: // * ~ * ~ * ~ * Jumping * ~ * ~ * ~ * //
            {
                /*
                Grounded <-
                -> Aerial
                */ 
                
                // Aerial
                currentState = playerState.Aerial;

                break;
            }
            case playerState.Aerial: // * ~ * ~ * ~ * Aerial * ~ * ~ * ~ * //
            {
                /*
                Grounded <-
                Jumping <-
                Dashing <-
                -> Grounded
                -> Dashing
                */

                // Grounded
                if(IsOnFloor()){
                    currentState = playerState.Grounded;
                }

                // Dashing

                break;
            }
            case playerState.Rolling: // * ~ * ~ * ~ * Rolling * ~ * ~ * ~ * //
            {
                /*
                Grounded <-
                Shoot <-
                -> Grounded
                -> Grabbing
                */ 
                break;
            }
            // case playerState.Vaulting:
            // {
            //     break;
            // }
            case playerState.Dashing: // * ~ * ~ * ~ * Dashing * ~ * ~ * ~ * //
            {
                /*
                Aerial <-
                Shoot <-
                -> Aerial
                -> Grabbing
                */ 
                break;
            }
            case playerState.Grabbing: // * ~ * ~ * ~ * Grabbing * ~ * ~ * ~ * //
            {
                /*
                Rolling <-
                Dashing <-
                -> Aiming
                */ 
                break;
            }
            case playerState.Aiming: // * ~ * ~ * ~ * Aiming * ~ * ~ * ~ * //
            {
                /*
                Grabbing <-
                -> Shooting
                */ 
                break;
            }
            case playerState.Shooting: // * ~ * ~ * ~ * Shooting * ~ * ~ * ~ * //
            {
                /*
                Aiming <-
                -> Rolling
                -> Dashing
                */ 
                break;
            }
        }
    }
    
    //********************************************************************//
    //                                                                    //
    //                               Physics                              //
    //                                                                    //
    //********************************************************************//
    public void processPhysics(){
        // Set horizontal velocity
        if(ifAction(currentState)) {
            velocity.x = getHorizontalInput() * moveSpeed;
            velocity.y += getGravity();
            if(currentState == playerState.Aerial){
                // Quickfall
                if(Input.IsActionJustPressed("down") && velocity.y/2 < quickFallInitialSpeed)
                    velocity.y = velocity.y * quickFallModifier + quickFallInitialSpeed;
            }
        }
        // Jumping        
        if(currentState == playerState.Jumping)
            velocity.y -= jumpSpeed;

        // TODO
        velocity = MoveAndSlide(velocity,FLOOR_NORMAL); // Need Vector2.Up to make sure floor detection works
    }   

    public float getHorizontalInput(){
        return Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
    }

    public float getGravity(){
        var gravityModifier = 1f;
        if(Input.IsActionPressed("down"))
            gravityModifier = downGravityModifier;
        else if(Input.IsActionPressed("jump"))
            gravityModifier = jumpGravityModifier;
        return gravity * gravityModifier * GetPhysicsProcessDeltaTime();
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }



    //********************************************************************//
    //                                                                    //
    //                               Test                                 //
    //                                                                    //
    //********************************************************************//
    private void printStateChange(){
        if(previousState != currentState){            
            GD.Print("playerState: " + currentState);
            previousState = currentState;
        }
    }
}
}

