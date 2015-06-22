﻿/*
 *  VolatilePhysics - A 2D Physics Library for Networked Games
 *  Copyright (c) 2015 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Volatile
{
  public class World
  {
    public IEnumerable<Shape> Shapes 
    { 
      get { return this.shapes.AsReadOnly(); } 
    }

    public IEnumerable<Body> Bodies 
    {
      get { return this.bodies.AsReadOnly(); } 
    }

    internal float Elasticity { get; private set; }

    protected List<Body> bodies;
    protected List<Shape> shapes;

    internal Vector2 gravity;
    internal float damping = 0.999f;

    // Each World instance should own its own object pools, in case
    // you want to run multiple World instances simultaneously.
    private Manifold.Pool manifoldPool;
    private Contact.Pool contactPool;
    private List<Manifold> manifolds;

    public World(Vector2 gravity, float damping = 0.999f)
    {
      this.bodies = new List<Body>();
      this.shapes = new List<Shape>();

      this.gravity = gravity;
      this.damping = damping;

      this.contactPool = new Contact.Pool();
      this.manifoldPool = new Manifold.Pool(this.contactPool);
      this.manifolds = new List<Manifold>();
    }

    /// <summary>
    /// Call this after adding all bodies.
    /// </summary>
    public virtual void Initialize()
    {
    }

    public virtual void AddBody(Body body)
    {
      foreach (Shape s in body.Shapes)
        this.shapes.Add(s);
      this.bodies.Add(body);
      body.World = this;
    }

    internal virtual void BroadPhase(
      List<Manifold> manifolds)
    {
      for (int i = 0; i < this.shapes.Count; i++)
        for (int j = i + 1; j < this.shapes.Count; j++)
          this.NarrowPhase(this.shapes[i], this.shapes[j], manifolds);
    }

    internal virtual void NarrowPhase(
      Shape sa, 
      Shape sb, 
      List<Manifold> manifolds)
    {
      if (sa.Body.CanCollide(sb.Body) == false)
        return;

      Shape.OrderShapes(ref sa, ref sb);
      Manifold manifold = Collision.Dispatch(sa, sb, this.manifoldPool);
      if (manifold != null)
        manifolds.Add(manifold);
    }

    public virtual IEnumerable<Shape> Query(AABB area)
    {
      foreach (Shape shape in this.shapes)
        if (shape.AABB.Intersect(area))
          yield return shape;
    }

    public virtual void Update()
    {
      this.UpdatePhysics();
    }

    protected void UpdatePhysics()
    {
      this.UpdateBodies();
      this.UpdateCollision();
      this.CleanupManifolds();
    }

    protected void UpdateBodies()
    {
      foreach (Body body in this.bodies)
        body.Update();
      this.BroadPhase(this.manifolds);
    }

    protected void CleanupManifolds()
    {
      for (int i = 0; i < this.manifolds.Count; i++)
      {
        this.manifolds[i].ReleaseContacts();
        this.manifoldPool.Release(this.manifolds[i]);
      }
      this.manifolds.Clear();
    }

    protected void UpdateCollision()
    {
      for (int i = 0; i < this.manifolds.Count; i++)
        this.manifolds[i].Prestep();

      this.Elasticity = 1.0f;
      for (int j = 0; j < Config.NUM_ITERATIONS * 1 / 3; j++)
        for (int i = 0; i < this.manifolds.Count; i++)
          this.manifolds[i].Solve();

      for (int i = 0; i < this.manifolds.Count; i++)
        this.manifolds[i].SolveCached();

      this.Elasticity = 0.0f;
      for (int j = 0; j < Config.NUM_ITERATIONS * 2 / 3; j++)
        for (int i = 0; i < this.manifolds.Count; i++)
          this.manifolds[i].Solve();
    }
  }
}
