﻿/*
 *  VolatilePhysics - A 2D Physics Library for Networked Games
 *  Copyright (c) 2015-2016 - Alexander Shoulson - http://ashoulson.com
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
  public sealed class Polygon : Shape
  {
    #region Factory Functions
    public static Polygon FromWorldVertices(
      Vector2 origin,
      Vector2 facing,
      Vector2[] vertices,
      float density = 1.0f,
      float friction = Config.DEFAULT_FRICTION,
      float restitution = Config.DEFAULT_RESTITUTION)
    {
      return new Polygon(
        origin,
        facing,
        Polygon.ComputeOffsetVertices(origin, facing, vertices),
        density,
        friction,
        restitution);
    }

    /// <summary>
    /// Assumes the shape is at the origin, facing right.
    /// </summary>
    public static Polygon FromLocalVertices(
      Vector2[] vertices,
      float density = 1.0f,
      float friction = Config.DEFAULT_FRICTION,
      float restitution = Config.DEFAULT_RESTITUTION)
    {
      return new Polygon(
        Vector2.zero,
        new Vector2(1.0f, 0.0f),
        vertices,
        density,
        friction,
        restitution);
    }
    #endregion

    #region Private Static Methods
    /// <summary>
    /// Converts world space vertices to offsets.
    /// </summary>
    /// <param name="origin">Shape origin point in world space.</param>
    /// <param name="facing">World space orientation of shape.</param>
    /// <param name="vertices">Vertex positions in world space.</param>
    /// <returns></returns>
    private static Vector2[] ComputeOffsetVertices(
      Vector2 origin,
      Vector2 facing,
      Vector2[] vertices)
    {
      Vector2[] offsets = new Vector2[vertices.Length];
      for (int i = 0; i < offsets.Length; i++)
        offsets[i] = ((vertices[i]) - origin).InvRotate(facing);
      return offsets;
    }

    /// <summary>
    /// Computes the vector between the vertex and the origin, given a 
    /// (rotation-adjusted) offset to that origin.
    /// </summary>
    private static Vector2 ComputeVertexOffset(
      Vector2 vertex,
      Vector2 originOffset,
      Vector2 shapeFacing)
    {
      return originOffset + shapeFacing.Rotate(vertex);
    }

    private static Axis[] ComputeAxes(Vector2[] vertices)
    {
      Axis[] axes = new Axis[vertices.Length];
      for (int i = 0; i < vertices.Length; i++)
      {
        Vector2 u = vertices[i];
        Vector2 v = vertices[(i + 1) % vertices.Length];
        Vector2 normal = (v - u).Left().normalized;
        axes[i] = new Axis(normal, Vector2.Dot(normal, u));
      }
      return axes;
    }

    private static float ComputeArea(Vector2[] vertices)
    {
      float sum = 0;

      for (int i = 0; i < vertices.Length; i++)
      {
        Vector2 v = vertices[i];
        Vector2 u = vertices[(i + 1) % vertices.Length];
        Vector2 w = vertices[(i + 2) % vertices.Length];
        sum += u.x * (v.y - w.y);
      }

      return sum / 2.0f;
    }

    private static float ComputeInertia(
      Vector2[] vertices,
      Vector2 originOffset,
      Vector2 shapeFacing)
    {
      float s1 = 0.0f;
      float s2 = 0.0f;

      // Compute the vertex offsets to the origin point
      Vector2[] vertexOffsets = new Vector2[vertices.Length];
      for (int i = 0; i < vertexOffsets.Length; i++)
        vertexOffsets[i] =
          ComputeVertexOffset(vertices[i], originOffset, shapeFacing);

      // Given the offsets, compute the inertia
      for (int i = 0; i < vertexOffsets.Length; i++)
      {
        Vector2 v = vertexOffsets[i];
        Vector2 u = vertexOffsets[(i + 1) % vertexOffsets.Length];
        float a = VolatileUtil.Cross(u, v);
        float b = v.sqrMagnitude + u.sqrMagnitude + Vector2.Dot(v, u);
        s1 += a * b;
        s2 += a;
      }

      return s1 / (6.0f * s2);
    }

    private static AABB ComputeBounds(Vector2[] vertices)
    {
      float top = vertices[0].y;
      float bottom = vertices[0].y;
      float left = vertices[0].x;
      float right = vertices[0].x;

      for (int i = 1; i < vertices.Length; i++)
      {
        top = Mathf.Max(top, vertices[i].y);
        bottom = Mathf.Min(bottom, vertices[i].y);
        left = Mathf.Min(left, vertices[i].x);
        right = Mathf.Max(right, vertices[i].x);
      }

      return new AABB(top, bottom, left, right);
    }

    private static Vector2[] CloneVertices(Vector2[] source)
    {
      Vector2[] vertices =
        new Vector2[source.Length];
      for (int i = 0; i < source.Length; i++)
        vertices[i] = source[i];
      return vertices;
    }

    private static Vector2[] CloneNormals(Axis[] source)
    {
      Vector2[] normals =
        new Vector2[source.Length];
      for (int i = 0; i < source.Length; i++)
        normals[i] = source[i].Normal;
      return normals;
    }
    #endregion

    #region Properties
    public override Shape.ShapeType Type { get { return ShapeType.Polygon; } }

    public override Vector2 Position { get { return this.origin; } }
    public override Vector2 Facing { get { return this.facing; } }
    public override float Angle { get { return this.facing.Angle(); } }

    public Vector2[] LocalVertices
    {
      get { return Polygon.CloneVertices(this.localVertices); }
    }

    public Vector2[] WorldVertices
    {
      get { return Polygon.CloneVertices(this.worldVertices); }
    }

    public Vector2[] LocalNormals
    {
      get { return Polygon.CloneNormals(this.localAxes); }
    }

    public Vector2[] WorldNormals
    {
      get { return Polygon.CloneNormals(this.worldAxes); }
    }
    #endregion

    #region Fields
    // Local space values
    internal Vector2[] localVertices;
    internal Axis[] localAxes;

    // World space values
    private Vector2 origin;
    private Vector2 facing;

    // Cached world space computation results -- these are cached and updated
    // every time the shape is moved (expensive, avoid doing this if you can)
    internal Vector2[] worldVertices;
    internal Axis[] worldAxes;
    #endregion

    #region Tests
    internal override bool ShapeQuery(
      Vector2 point,
      bool useLocalSpace = false)
    {
      Axis[] axes = this.GetAxes(useLocalSpace);

      foreach (Axis axis in axes)
        if (Vector2.Dot(axis.Normal, point) > axis.Width)
          return false;
      return true;
    }

    internal override bool ShapeQuery(
      Vector2 point,
      float radius,
      bool useLocalSpace = false)
    {
      Axis[] axes = this.GetAxes(useLocalSpace);
      Vector2[] vertices = this.GetVertices(useLocalSpace);

      // Get the axis on the polygon closest to the circle's origin
      float penetration;
      int foundIndex =
        Collision.FindAxisMaxPenetration(
          point,
          radius,
          axes,
          out penetration);

      if (foundIndex < 0)
        return false;

      Vector2 a = vertices[foundIndex];
      Vector2 b = vertices[(foundIndex + 1) % vertices.Length];
      Axis axis = axes[foundIndex];

      // If the circle is past one of the two vertices, check it like
      // a circle-circle intersection where the vertex has radius 0
      float d = VolatileUtil.Cross(axis.Normal, point);
      if (d > VolatileUtil.Cross(axis.Normal, a))
        return Collision.TestPointCircleSimple(a, point, radius);
      if (d < VolatileUtil.Cross(axis.Normal, b))
        return Collision.TestPointCircleSimple(b, point, radius);
      return true;
    }

    internal override bool ShapeRayCast(
      ref RayCast ray,
      ref RayResult result)
    {
      Axis[] axes = this.GetAxes(ref ray);
      Vector2[] vertices = this.GetVertices(ref ray);

      int foundIndex = -1;
      float inner = float.MaxValue;
      float outer = 0;
      bool couldBeContained = true;

      for (int i = 0; i < vertices.Length; i++)
      {
        Axis curAxis = axes[i];

        // Distance between the ray origin and the axis/edge along the normal
        // (i.e., shortest distance between ray origin and the edge).
        float proj = Vector2.Dot(curAxis.Normal, ray.Origin) - curAxis.Width;

        // See if the point is outside of any of the axes
        if (proj > 0.0f)
          couldBeContained = false;

        // Projection of the ray direction onto the axis normal (use negative
        // normal because we want to get the penetration length).
        float slope = Vector2.Dot(-curAxis.Normal, ray.Direction);

        if (slope == 0.0f)
          continue;
        float dist = proj / slope;

        // The ray is pointing opposite the edge normal (towards the edge)
        if (slope > 0.0f)
        {
          if (dist > inner)
          {
            return false;
          }
          if (dist > outer)
          {
            outer = dist;
            foundIndex = i;
          }
        }
        // The ray is pointing along the edge normal (away from the edge)
        else
        {
          if (dist < outer)
          {
            return false;
          }
          if (dist < inner)
          {
            inner = dist;
          }
        }
      }

      if (couldBeContained == true)
      {
        result.SetContained(this);
        return true;
      }
      else if (foundIndex >= 0 && outer <= ray.Distance)
      {
        result.Set(
          this,
          outer,
          // N.B.: For historical raycasts this normal will be wrong!
          // Must be either transformed back to world or invalidated later.
          axes[foundIndex].Normal);
        return true;
      }

      return false;
    }

    internal override bool ShapeCircleCast(
      ref RayCast ray,
      float radius,
      ref RayResult result)
    {
      Axis[] axes = this.GetAxes(ref ray);
      Vector2[] vertices = this.GetVertices(ref ray);

      bool checkVertices =
        this.CircleCastVertices(
          ref ray,
          radius,
          ref result,
          vertices);

      bool checkEdges =
        this.CircleCastEdges(
          ref ray,
          radius,
          ref result,
          axes,
          vertices);

      return checkVertices || checkEdges;
    }
    #endregion

    public override void SetWorld(Vector2 position, Vector2 facing)
    {
      this.origin = position;
      this.facing = facing;
      this.ComputeWorldVertices();
      this.AABB = Polygon.ComputeBounds(this.worldVertices);
    }

    #region Internals
    /// <summary>
    /// Creates a new polygon from an origin and local-space vertices.
    /// </summary>
    /// <param name="origin">Shape origin point in world space.</param>
    /// <param name="facing">World space orientation of shape.</param>
    /// <param name="vertices">Vertex positions relative to the origin.</param>
    /// <param name="density">Shape density.</param>
    /// <param name="friction">Shape friction.</param>
    /// <param name="restitution">Shape restitution.</param>
    private Polygon(
      Vector2 origin,
      Vector2 facing,
      Vector2[] vertices,
      float density,
      float friction,
      float restitution)
      : base(density, friction, restitution)
    {
      this.origin = origin;
      this.facing = facing;
      this.localVertices = vertices;

      this.localAxes = Polygon.ComputeAxes(this.localVertices);
      this.worldVertices = new Vector2[this.localVertices.Length];
      this.worldAxes = new Axis[this.localVertices.Length];

      // Defined in Shape class
      this.Area = Polygon.ComputeArea(vertices);

      this.ComputeWorldVertices();
    }

    /// <summary>
    /// Computes inertia given an offset from the origin.
    /// </summary>
    internal override float ComputeInertia(Vector2 offset)
    {
      return Polygon.ComputeInertia(this.localVertices, offset, this.facing);
    }

    /// <summary>
    /// Used in collision, for consistency.
    /// </summary>
    internal bool ContainsPoint(Vector2 point)
    {
      return this.ShapeQuery(point);
    }

    /// <summary>
    /// Special case that ignores axes pointing away from the normal.
    /// </summary>
    internal bool ContainsPointPartial(Vector2 point, Vector2 normal)
    {
      foreach (Axis axis in this.worldAxes)
        if (Vector2.Dot(axis.Normal, normal) >= 0.0f &&
            Vector2.Dot(axis.Normal, point) > axis.Width)
          return false;
      return true;
    }

    /// <summary>
    /// Coverts the local space axes and vertices to world space.
    /// </summary>
    private void ComputeWorldVertices()
    {
      for (int i = 0; i < this.localVertices.Length; i++)
      {
        this.worldVertices[i] =
          this.origin + this.localVertices[i].Rotate(this.facing);

        Vector2 normal = this.localAxes[i].Normal.Rotate(this.facing);
        float dot =
          Vector2.Dot(normal, this.origin) +
          this.localAxes[i].Width;
        this.worldAxes[i] = new Axis(normal, dot);
      }
    }

    private bool CircleCastEdges(
      ref RayCast ray,
      float radius,
      ref RayResult result,
      Axis[] axes,
      Vector2[] vertices)
    {
      int foundIndex = -1;
      int length = vertices.Length;
      bool couldBeContained = true;

      // Pre-compute and initialize values
      float shortestDist = float.MaxValue;
      Vector2 v3 = ray.Direction.Left();

      // Check the edges -- this will be different from the raycast because
      // we care about staying within the ends of the edge line segment
      for (int i = 0; i < vertices.Length; i++)
      {
        Axis curAxis = axes[i];

        // Push the edges out by the radius
        Vector2 extension = curAxis.Normal * radius;
        Vector2 a = vertices[i] + extension;
        Vector2 b = vertices[(i + 1) % length] + extension;

        // Update the check for containment
        if (couldBeContained == true)
        {
          float proj = Vector2.Dot(curAxis.Normal, ray.Origin) - curAxis.Width;

          // The point lies outside of the outer layer
          if (proj > radius)
          {
            couldBeContained = false;
          }
          // The point lies between the outer and inner layer
          else if (proj > 0.0f)
          {
            // See if the point is within the center Vornoi region of the edge
            float d = VolatileUtil.Cross(curAxis.Normal, ray.Origin);
            if (d > VolatileUtil.Cross(curAxis.Normal, a))
              couldBeContained = false;
            if (d < VolatileUtil.Cross(curAxis.Normal, b))
              couldBeContained = false;
          }
        }

        // For the cast, only consider rays pointing towards the edge
        if (Vector2.Dot(curAxis.Normal, ray.Direction) >= 0.0f)
          continue;

        // See: 
        // https://rootllama.wordpress.com/2014/06/20/ray-line-segment-intersection-test-in-2d/
        Vector2 v1 = ray.Origin - a;
        Vector2 v2 = b - a;

        float denominator = Vector2.Dot(v2, v3);
        float t1 = VolatileUtil.Cross(v2, v1) / denominator;
        float t2 = Vector2.Dot(v1, v3) / denominator;

        if ((t2 >= 0.0f) && (t2 <= 1.0f) && (t1 > 0.0f) && (t1 < shortestDist))
        {
          // See if the point is outside of any of the axes
          shortestDist = t1;
          foundIndex = i;
        }
      }

      // Report results
      if (couldBeContained == true)
      {
        result.SetContained(this);
        return true;
      }
      else if (foundIndex >= 0 && shortestDist <= ray.Distance)
      {
        result.Set(
          this,
          shortestDist,
          // N.B.: For historical raycasts this normal will be wrong!
          // We will invalidate the value later. It just isn't worth
          // transforming the normal back to world space for reporting.
          this.worldAxes[foundIndex].Normal);
        return true;
      }
      return false;
    }

    private bool CircleCastVertices(
      ref RayCast ray,
      float radius,
      ref RayResult result,
      Vector2[] vertices)
    {
      float sqrRadius = radius * radius;
      bool castHit = false;

      for (int i = 0; i < vertices.Length; i++)
      {
        castHit |=
          Collision.CircleRayCast(
            this,
            vertices[i],
            sqrRadius,
            ref ray,
            ref result);
        if (result.IsContained == true)
          return true;
      }

      return castHit;
    }

    private Axis[] GetAxes(bool useLocalSpace)
    {
      return (useLocalSpace ? this.localAxes : this.worldAxes);
    }

    private Axis[] GetAxes(ref RayCast ray)
    {
      return (ray.IsLocalSpace ? this.localAxes : this.worldAxes);
    }

    private Vector2[] GetVertices(bool useLocalSpace)
    {
      return (useLocalSpace ? this.localVertices : this.worldVertices);
    }

    private Vector2[] GetVertices(ref RayCast ray)
    {
      return (ray.IsLocalSpace ? this.localVertices : this.worldVertices);
    }
    #endregion

    #region Debug
    public override void GizmoDraw(
      Color edgeColor,
      Color normalColor,
      Color originColor,
      Color aabbColor,
      float normalLength)
    {
      Color current = Gizmos.color;

      Vector2[] worldNormals = this.WorldNormals;
      for (int i = 0; i < this.worldVertices.Length; i++)
      {
        Vector2 u = this.worldVertices[i];
        Vector2 v =
          this.worldVertices[(i + 1) % this.worldVertices.Length];
        Vector2 n = worldNormals[i];

        Vector2 delta = v - u;
        Vector2 midPoint = u + (delta * 0.5f);

        // Draw edge
        Gizmos.color = edgeColor;
        Gizmos.DrawLine(u, v);

        // Draw normal
        Gizmos.color = normalColor;
        Gizmos.DrawLine(midPoint, midPoint + (n * normalLength));

        // Draw line to origin
        Gizmos.color = originColor;
        Gizmos.DrawLine(u, this.Position);
      }

      // Draw facing
      Gizmos.color = normalColor;
      Gizmos.DrawLine(
        this.Position,
        this.Position + this.Facing * normalLength);

      // Draw origin
      Gizmos.color = originColor;
      Gizmos.DrawWireSphere(this.Position, 0.05f);

      this.AABB.GizmoDraw(aabbColor);

      Gizmos.color = current;
    }
    #endregion
  }
}