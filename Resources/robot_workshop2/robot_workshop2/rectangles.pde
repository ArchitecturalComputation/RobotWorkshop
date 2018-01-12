class rectangles
{
  float angle;
  int xValue;
  int yValue;
  
  rectangles(float _angle, int _xValue, int _yValue)
  {
    angle=_angle;
    xValue=_xValue;
    yValue=_yValue;
  }
  void display()
  {
    rectMode(CENTER);
    pushMatrix();
    translate(xValue, yValue);
    fill(0);
    rotate(angle);
    rect(0, 0, 45, 15);
    popMatrix();
   
    
  }
}