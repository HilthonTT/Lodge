type UserAuth = {
  id: string;
  email: string;
  name: string;
  imageId?: string;
};

type LoginRequest = {
  email: string;
  password: string;
};
