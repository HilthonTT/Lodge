type Props = {
  title: string;
  description: string;
  icon: React.ReactNode;
};

export const InfoCard = ({ title, description, icon: Icon }: Props) => {
  return (
    <div className="flex items-center">
      {Icon}
      <div className="flex flex-col items-start ml-4">
        <p className="font-semibold">{title}</p>
        <p className="text-muted-foreground">{description}</p>
      </div>
    </div>
  );
};
