export interface ConstellationProps {
  name: string;
  points: [
    {
      sourceId: string;
      x: number;
      y: number;
      z: number;
    }
  ];
}
