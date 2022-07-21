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

    [Export]
    public int moveAcceleration = 2500; // pixels/s
    [Export]
    public float groundInputDrag = 0.95f; // pixels/s
    [Export]
    public float groundNoInputDrag = 0.8f; // pixels/s
    [Export]
    public float AerialInputDrag = 0.95f; // pixels/s
    [Export]
    public float AerialNoInputDrag = 0.988f; // pixels/s
    // [Export]
    // public int maxActionSpeed = 500; // pixels/s
    // [Export]
    // public int gravity = 2000; // pixels/s^2
    [Export]
    public int jumpSpeed = 850; // pix/s
    [Export]
    public int rollSpeed = 700; // pix/s
    // [Export]
    // public float jumpGravityModifier = 0.6f;
    // [Export]
    // public float downGravityModifier = 1.6f;
    [Export]
    public int minimumStompBounce = 400;
    // [Export]
    // public int quickFallInitialSpeed = 300;
    // [Export]
    // public float quickFallModifier = 1f;
    [Export]
    public int dodgeGravity = 800;
    // Player Physics
    [Export]
    public float coyoteTime = 0.12f; // seconds of cyotyeTime

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

    public bool hasDodged = false;

    //********************************************************************//
    //                                                                    //
    //                               Ready                                //
    //                                                                    //
    //********************************************************************//

    public override void _Ready()
    {        
        thisType = entityType.player;

        // enemyStomped += onEnemyStomped;

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

    public void onRollingDuration_TimeOut(){
        currentState = playerState.Grounded;
        GetNode<Light2D>("TestLight").Enabled = false;
    }

    public void onCoyoteTimeDuration_TimeOut(){
        GD.Print("onCoyoteTimeDuration_TimeOut\n");
        currentState = playerState.Aerial;
    }

    // public void onJumpBuffer_TimeOut(){
    //     GD.Print("Time Left on Time Out: " + GetNode<Timer>("JumpBuffer").TimeLeft + "\n");
    // }

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

                if(IsOnFloor())
                    hasDodged = false;

                // Jumping
                if(Input.IsActionJustPressed("jump") || GetNode<Timer>("JumpBuffer").TimeLeft > 0){
                    if(GetNode<Timer>("JumpBuffer").TimeLeft > 0)
                        GD.Print("\nUsed Jump Buffer\n");
                    GetNode<Timer>("JumpBuffer").Stop();
                    currentState = playerState.Jumping;
                }
                
                var nodeCoyoteTimeDuration = GetNode<Timer>("CoyoteTimeDuration");
                // Aerial
                if(!IsOnFloor() && nodeCoyoteTimeDuration.TimeLeft == 0){
                    GD.Print("Start CoyoteTime\nhasDodged:" + hasDodged + "\n");
                    nodeCoyoteTimeDuration.WaitTime = coyoteTime;
                    nodeCoyoteTimeDuration.Start();
                }
                // Rolling
                if(Input.IsActionJustPressed("dodge") && IsOnFloor() && getIfHorizontalInputPressed()){ // Can't roll durring coyoteTime
                    nodeCoyoteTimeDuration.Stop();
                    //GD.Print("Time Left: " + nodeCoyoteTimeDuration.TimeLeft + "\n");
                    groundedToRolling();
                }


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
                Grounded <- // onCoyoteTimeDuration_TimeOut()
                Jumping <-
                Dashing <-
                -> Grounded
                */

                // Grounded
                if(IsOnFloor()){
                    currentState = playerState.Grounded;
                }

                // // Dashing
                // if(Input.IsActionJustPressed("dodge") && getIfHorizontalInputPressed()){ // Can't roll durring coyoteTime
                //     currentState = playerState.Dashing;
                //     // GD.Print("Time Left: " + nodeCoyoteTimeDuration.TimeLeft + "\n");
                // }

                break;
            }
            case playerState.Rolling: // * ~ * ~ * ~ * Rolling * ~ * ~ * ~ * //
            {
                /*
                Grounded <-
                Shoot <-
                -> Grounded // onRollingDuration_TimeOut()
                -> Grabbing
                */

                // Jump Buffer out of Rolling
                if(Input.IsActionJustPressed("jump"))
                    GetNode<Timer>("JumpBuffer").Start();

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
                -> Aerial   // onRollingDuration_TimeOut()
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

    // Start Rolling
    public void groundedToRolling(){
        currentState = playerState.Rolling;

        hasDodged = true;
        GetNode<Timer>("RollingDuration").Start();
        GetNode<Light2D>("TestLight").Enabled = true;

        var horizontalInput = getHorizontalInput();
        if(horizontalInput < 0){
            velocity.x = -rollSpeed;
        }
        else if(horizontalInput > 0){
            velocity.x = rollSpeed;
        }
        velocity.y = 0;
    }

    //********************************************************************//
    //                                                                    //
    //                               Physics                              //
    //                                                                    //
    //********************************************************************//
    public void processPhysics(){
        var delta = GetPhysicsProcessDeltaTime();
        // Set horizontal velocity
        if(ifAction(currentState)) {
            velocity.x += getMoveAcceleration(delta);
            //velocity.x.
            
            velocity.y += gravity * delta; // getGravity() for weaker or stronger gravity
            if(currentState == playerState.Grounded){
                if(getIfHorizontalInputPressed()){
                    velocity.x *= groundInputDrag;
                }
                else {
                    velocity.x *= groundNoInputDrag;
                }
            }
            else if(currentState == playerState.Aerial){
                if(getIfHorizontalInputPressed()){
                    velocity.x *= AerialInputDrag;
                }
                else {
                    velocity.x *= AerialNoInputDrag;
                }
                // Quickfall
                // if(Input.IsActionJustPressed("down") && velocity.y/2 < quickFallInitialSpeed)
                //     velocity.y = velocity.y * quickFallModifier + quickFallInitialSpeed;
            }
            //velocity.x = maxMagnitude(velocity.x,maxActionSpeed);
        }
        // Jumping        
        if(currentState == playerState.Jumping){
            velocity.y = -jumpSpeed; // -velocity.y for coyote time
        }

        if(ifDodge(currentState)){
            //GD.Print("Dodge");
            velocity.y += dodgeGravity * GetPhysicsProcessDeltaTime();
        }        

        velocity = MoveAndSlide(velocity,FLOOR_NORMAL); // Need Vector2.Up to make sure floor detection works
    }   

    public float getHorizontalInput(){
        return Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
    }

    public bool getIfHorizontalInputPressed(){
        var left = Input.IsActionPressed("move_left");
        var right = Input.IsActionPressed("move_right");
        return !(left && right) && (left || right); // XOR
    }

    public float getMoveAcceleration(float delta){
        return getHorizontalInput() * moveAcceleration * delta;
    }

    // public float getGravity(){
    //     var gravityModifier = 1f;
    //     if(Input.IsActionPressed("down"))
    //         gravityModifier = downGravityModifier;
    //     else if(Input.IsActionPressed("jump"))
    //         gravityModifier = jumpGravityModifier;
    //     return gravity * gravityModifier * GetPhysicsProcessDeltaTime();
    // }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

    //********************************************************************//
    //                                                                    //
    //                              Tools                                 //
    //                                                                    //
    //********************************************************************//

    public float maxMagnitude(float value, float magnitude){
        if(value < -magnitude)
            return -magnitude;
        else if(value > magnitude)
            return magnitude;
        else
            return value;
    }


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

