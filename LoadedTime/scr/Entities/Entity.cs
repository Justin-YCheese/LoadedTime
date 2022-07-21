using Godot;
using System;

namespace Entities
{
public class Entity : KinematicBody2D
{
    public Vector2 velocity = Vector2.Zero;
    
    // [Export]
    // public int moveSpeed;
    [Export]
    public int gravity;

    public Vector2 FLOOR_NORMAL = Vector2.Up;
    
    public enum entityType
    {
        none = 0,
        player = 1,
        enemy = 2
    }
    public entityType thisType = entityType.none;
}

}