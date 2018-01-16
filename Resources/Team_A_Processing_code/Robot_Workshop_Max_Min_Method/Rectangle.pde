class Rectangle
{
  float x, y;
  float orientation;

  Rectangle(float xposition, float yposition, float orient)
  {
    x = xposition;
    y = yposition;
    orientation = orient;
  }

  void display()
  {
    fill(0);
    noStroke();
    pushMatrix();
    translate(x, y);
    rotate(orientation);
    rect(0, 0, 180, 60);
    popMatrix();
  }
}